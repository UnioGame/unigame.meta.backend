namespace MetaService.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using Game.Modules.ModelMapping;
    using Newtonsoft.Json;
    using R3;
    using Shared;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UniGame.GameFlow.Runtime;
    using UniGame.Runtime.DateTime;
     
    using UnityEngine;

    [Serializable]
    public class BackendMetaService : GameService, IBackendMetaService
    {
#if UNITY_EDITOR
        public static BackendMetaService EditorInstance;
#endif

        private readonly bool _useDefaultProvider;
        private IRemoteMetaDataConfiguration _metaDataConfiguration;
        private IRemoteMetaProvider _defaultMetaProvider;
        private BackendTypeId _defaultProviderId;
        private IDictionary<int, IRemoteMetaProvider> _metaProviders;
        private Dictionary<int,ContractDataResult> _responceCache;
        private Dictionary<int,RemoteMetaData> _metaIdCache;
        private Dictionary<Type, IRemoteMetaProvider> _contractsCache;
        private Subject<ContractDataResult> _dataStream;
        private List<IMetaContractHandler> _contractHandlers = new();
        private IRemoteDataConverter _defaultConverter;
        private int _historySize;
        private int _historyIndex;
        private ContractHistoryItem[] _history;

        public BackendMetaService(
            bool useDefaultProvider,
            int historySize,
            BackendTypeId defaultMetaProvider,
            IDictionary<int,IRemoteMetaProvider> metaProviders,
            IRemoteMetaDataConfiguration metaDataConfiguration)
        {
            BackendMetaServiceExtensions.RemoteMetaService = this;

            _defaultConverter = metaDataConfiguration.Converter ?? new JsonRemoteDataConverter();
            _responceCache = new Dictionary<int, ContractDataResult>(64);
            _metaIdCache = new Dictionary<int, RemoteMetaData>(64);
            _contractsCache = new Dictionary<Type, IRemoteMetaProvider>(64);
            _dataStream = new Subject<ContractDataResult>().AddTo(LifeTime);
            _historySize = historySize;
            _history = new ContractHistoryItem[_historySize];

            _useDefaultProvider = useDefaultProvider;
            _metaDataConfiguration = metaDataConfiguration;
            
            _defaultProviderId = defaultMetaProvider;

            _defaultMetaProvider = metaProviders[defaultMetaProvider];
            _metaProviders = metaProviders;

            _dataStream.Subscribe(AddHistoryItem).AddTo(LifeTime);

            UpdateMetaCache();
            
#if UNITY_EDITOR
            EditorInstance = this;
#endif
        }
        
        public ContractHistoryItem[] ContractHistory => _history;
        
        public Observable<ContractDataResult> DataStream => _dataStream;

        public bool AddContractHandler(IMetaContractHandler handler)
        {
            if (_contractHandlers.Contains(handler)) return false;
            _contractHandlers.Add(handler);
            return true;
        }

        public bool RemoveContractHandler<T>() where T : IMetaContractHandler
        {
            var count = _contractHandlers.RemoveAll(x => x is T);
            return count > 0;
        }

        public IRemoteMetaDataConfiguration MetaDataConfiguration => _metaDataConfiguration;
        
        public IRemoteMetaProvider FindProvider(MetaContractData data)
        {
            var contract = data.contract;
            var contractType = contract.GetType();
            var meta = data.metaData;
            var providerId = meta.overrideProvider ? meta.provider : _defaultProviderId;
            
            if(_contractsCache.TryGetValue(contractType,out var provider))
                return provider;

            IRemoteMetaProvider resultProvider = null;
            
            if (_metaProviders.TryGetValue(providerId, out var contractProvider))
                return contractProvider;
            
            if(_defaultMetaProvider.IsContractSupported(contract))
                resultProvider = _defaultMetaProvider;

            if (resultProvider == null)
            {
                foreach (var metaProvider in _metaProviders.Values)
                {
                    if(!metaProvider.IsContractSupported(data.contract))
                        continue;
                    resultProvider = metaProvider;
                    break;
                }
            }
            
            resultProvider ??= _defaultMetaProvider;
            _contractsCache[contractType] = resultProvider;
            
            return resultProvider;
        }     
        
        public IRemoteMetaProvider GetProvider(int providerId)
        {
            return _metaProviders.TryGetValue(providerId, out var provider) 
                ? provider : _defaultMetaProvider;
        }        
        
        public bool RegisterProvider(int providerId,IRemoteMetaProvider provider)
        {
            _metaProviders[providerId] = provider;
            return true;
        }
        
        public void SwitchProvider(int providerId)
        {
            _defaultMetaProvider = GetProvider(providerId);
        }

        public IRemoteMetaProvider SelectProvider(RemoteMetaData meta,IRemoteMetaContract contract)
        {
            if (meta.overrideProvider)
                return GetProvider(meta.provider);

            if (_useDefaultProvider && _defaultMetaProvider.IsContractSupported(contract))
                return _defaultMetaProvider;

            foreach (var metaProvider in _metaProviders)
            {
                var provider = metaProvider.Value;
                if(provider.IsContractSupported(contract))
                    return provider;
            }
            
            return _defaultMetaProvider;
        }

        public async UniTask<ContractDataResult> ExecuteAsync(IRemoteMetaContract contract,CancellationToken cancellation = default)
        {
            var meta = FindMetaData(contract);
            
            var provider = SelectProvider(meta,contract);
            
            var contractData = new MetaContractData()
            {
                id = meta.id,
                metaData = meta,
                contract = contract,
                provider = provider,
                contractName = contract.Path,
            };
            
            return await ExecuteAsync(contractData, cancellation)
                .AttachExternalCancellation(LifeTime.Token);
        }

        public async UniTask<ContractDataResult> ExecuteAsync(
            MetaContractData contractData,
            CancellationToken cancellation = default)
        {
            try
            {
                var meta = contractData.metaData;
                var provider = contractData.provider ?? FindProvider(contractData);
                contractData.provider = provider;
                contractData.contractName ??= meta.method;
                
                var contract = contractData.contract;
                
                var connectionResult = await ConnectAsync(provider,cancellation);
                if(connectionResult.Success == false) 
                    return ContractDataResult.Empty;
                
                if(!provider.IsContractSupported(contract))
                    return BackendMetaConstants.UnsupportedContract;

                var contractValue = contractData.contract;
                foreach (var contractHandler in _contractHandlers)
                    contractValue = contractHandler.UpdateContract(contractValue); 
                
                contractData.contract = contractValue;
                
                var remoteResult = await provider.ExecuteAsync(contractData,cancellation);

                await UniTask.SwitchToMainThread();
                
                var result = ProcessRemoteResponse(contractData,remoteResult);

                _responceCache.TryGetValue(result.metaId, out var response);
                _responceCache[result.metaId] = result;
                
                var isChanged = response == null || response.hash != result.hash;
                if(isChanged && result.success) 
                    _dataStream.OnNext(response);
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return ContractDataResult.Empty;
            }
        }
        
        public RemoteMetaData FindMetaData(IRemoteMetaContract contract)
        {
            foreach (var meta in _metaDataConfiguration.RemoteMetaData)
            {
#if UNITY_EDITOR
                if (meta.contract == null)
                {
                    Debug.LogError($"Backend Service: {meta.id} {meta.method} {meta.provider}");
                    return null;
                }
#endif
                
                if(meta.contract.GetType() == contract.GetType())
                    return meta;
            }
            
            return RemoteMetaData.Empty;
        }
        
        public RemoteMetaData FindMetaData(int metaId)
        {
            if (_metaIdCache.TryGetValue(metaId, out var metaData))
                return metaData;
            return RemoteMetaData.Empty;
        }
        

        public ContractDataResult ProcessRemoteResponse(MetaContractData contractData, RemoteMetaResult response)
        {
            var remoteId = contractData.contractName;
            var contract = contractData.contract;
            var metaData = contractData.metaData;
            var success = response.success;
            
            var responseData = response.data ?? string.Empty;
            var unixTime = DateTime.Now.ToUnixTimestamp();
            var outputType = contract.OutputType;
            
            outputType = outputType == null || outputType == typeof(VoidRemoteData) 
                ? typeof(string)
                : outputType;
            
            var resultType = response.data !=null && success
                ? response.data.GetType()
                : outputType;
            
            var resultObject = responseData;
            
            switch (outputType)
            {
                case not null when outputType == typeof(string):
                    resultObject = responseData;
                    break;
                case not null when outputType != typeof(string) && resultType == typeof(string):
                    if (responseData is string responseString &&
                        !string.IsNullOrEmpty(responseString))
                    {
                        resultObject = _defaultConverter.Convert(outputType, responseString);
                    }
                    break;
                case not null when outputType == typeof(VoidRemoteData):
                    resultObject = VoidRemoteData.Empty;
                    break;
            }
            
            var result = new ContractDataResult()
            {
                contractId = contract.Path,
                metaId = metaData.id,
                payload = contract?.Payload,
                resultType = outputType,
                model = resultObject,
                result = responseData,
                success = response.success,
                hash = responseData.GetHashCode(),
                error = response.error,
                timestamp = unixTime,
            };

#if GAME_DEBUG && UNITY_EDITOR
            if (!response.success)
            {
                Debug.LogError($"Remote Meta Service: remote: {remoteId} payload {contract?.GetType().Name} | error: {response.error} | method: {contract.Path}");
            }
#endif
            
            return result;
        }
        
        private async UniTask<MetaConnectionResult> ConnectAsync(IRemoteMetaProvider provider, CancellationToken cancellation = default)
        {
            if (provider.State.CurrentValue != ConnectionState.Connected)
            {
                var connectionResult = await provider.ConnectAsync()
                    .AttachExternalCancellation(cancellation);
                return connectionResult;
            }

            return new MetaConnectionResult()
            {
                Success = true,
                Error = string.Empty,
                State = ConnectionState.Connected,
            };
        }

        private void UpdateMetaCache()
        {
            _metaIdCache.Clear();
            
            var items = _metaDataConfiguration.RemoteMetaData;
            foreach (var metaData in items)
            {
                if(_metaIdCache.TryGetValue((RemoteMetaId)metaData.id,out var _))
                    continue;
                _metaIdCache[(RemoteMetaId)metaData.id] = metaData;
            }
        }
        
        private void AddHistoryItem(ContractDataResult result)
        {
            var id = _historyIndex;
            _historyIndex++;
            
            var historyItem = new ContractHistoryItem()
            {
                id =id,
                result = result,
            };
            
            var index = id % _historySize;
            _history[index] = historyItem;
        }

        private RemoteMetaData CreateNewRemoteMeta(string methodName)
        {
            var contract = new SimpleMetaContract<string, string>();
            var id = _metaDataConfiguration.CalculateMetaId(contract);
            
            return new RemoteMetaData()
            {
                id = id,
                method = methodName,
                contract = contract,
            };
        }

    }
}