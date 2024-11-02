namespace MetaService.Runtime
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using Game.Modules.ModelMapping;
    using Shared;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.UniNodes.GameFlow.Runtime;
    using UniModules.UniCore.Runtime.DateTime;
    using UniRx;
    using UnityEngine;

    [Serializable]
    public class BackendMetaService : GameService,IBackendMetaService
    {
        private IRemoteMetaDataConfiguration _metaDataConfiguration;
        private IRemoteMetaProvider _defaultMetaProvider;
        private IDictionary<int, IRemoteMetaProvider> _metaProviders;
        private Dictionary<int,MetaDataResult> _responceCache;
        private Dictionary<int,RemoteMetaData> _metaIdCache;
        private Dictionary<Type, IRemoteMetaProvider> _contractsCache;
        private Subject<MetaDataResult> _dataStream;
        private List<IMetaContractHandler> _contractHandlers = new();
        private string _connectionId = string.Empty;

        public BackendMetaService(IRemoteMetaProvider defaultMetaProvider,
            IDictionary<int,IRemoteMetaProvider> metaProviders,
            IRemoteMetaDataConfiguration metaDataConfiguration)
        {
            BackendMetaServiceExtensions.RemoteMetaService = this;
            
            _responceCache = new Dictionary<int, MetaDataResult>(64);
            _metaIdCache = new Dictionary<int, RemoteMetaData>(64);
            _contractsCache = new Dictionary<Type, IRemoteMetaProvider>(64);
            _dataStream = new Subject<MetaDataResult>().AddTo(LifeTime);
            
            _metaDataConfiguration = metaDataConfiguration;
            _defaultMetaProvider = defaultMetaProvider;
            _metaProviders = metaProviders;

            InitializeCache();
        }

        public IObservable<MetaDataResult> DataStream => _dataStream;

        public IReadOnlyReactiveProperty<ConnectionState> State => _defaultMetaProvider.State;

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
            var providerId = meta.provider;
            
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
            if (_metaProviders.TryGetValue(providerId, out var provider))
                return provider;
            return _defaultMetaProvider;
        }        
        
        public bool RegisterProvider(int providerId,IRemoteMetaProvider provider)
        {
            return _metaProviders.TryAdd(providerId, provider);
        }
        
        public async UniTask<MetaConnectionResult> ConnectAsync()
        {
            _connectionId = SystemInfo.deviceUniqueIdentifier;
#if UNITY_EDITOR
            Debug.Log($"BackendMetaService ConnectAsync with deviceId: {_connectionId}");
#endif
            return await ConnectAsync(_defaultMetaProvider);
        }

        public async UniTask DisconnectAsync()
        {
            await _defaultMetaProvider.DisconnectAsync();
        }

        public void SwitchProvider(int providerId)
        {
            _defaultMetaProvider = GetProvider(providerId);
        }

        public async UniTask<MetaDataResult> ExecuteAsync(IRemoteMetaContract contract)
        {
            var meta = FindMetaData(contract);
            if (meta == RemoteMetaData.Empty) 
                return MetaDataResult.Empty;

            var provider = GetProvider(meta.provider);
            
            var contractData = new MetaContractData()
            {
                id = meta.id,
                metaData = meta,
                contract = contract,
                provider = provider,
                contractName = meta.method,
            };
            
            return await ExecuteAsync(contractData);
        }
        
        private async UniTask<MetaDataResult> ExecuteAsync(MetaContractData contractData)
        {
            try
            {
                var meta = contractData.metaData;
                var provider = contractData.provider ?? FindProvider(contractData);
                contractData.provider = provider;
                contractData.contractName ??= meta.method;
                
                var contract = contractData.contract;
                
                var connectionResult = await ConnectAsync(provider);
                if(connectionResult.Success == false) 
                    return MetaDataResult.Empty;
                
                if(!provider.IsContractSupported(contract))
                    return BackendMetaConstants.UnsupportedContract;

                var contractValue = contractData.contract;
                foreach (var contractHandler in _contractHandlers)
                    contractValue = contractHandler.UpdateContract(contractValue); 
                
                contractData.contract = contractValue;
                var remoteResult = await provider.ExecuteAsync(contractData);

                var result = RegisterRemoteResult(contractData,remoteResult);

                _responceCache.TryGetValue(result.id, out var response);
                _responceCache[result.id] = result;
                
                var isChanged = response == null || response.hash != result.hash;
                if(isChanged && result.success) 
                    _dataStream.OnNext(response);
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return MetaDataResult.Empty;
            }
        }
        
        public RemoteMetaData FindMetaData<TContract>(TContract contract)
            where TContract : IRemoteMetaContract
        {
            foreach (var meta in _metaDataConfiguration.RemoteMetaData)
            {
                if(meta.contract.OutputType == contract.OutputType && 
                   meta.contract.InputType == contract.InputType)
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

                
        private async UniTask<MetaConnectionResult> ConnectAsync(IRemoteMetaProvider provider)
        {
            if (provider.State.Value != ConnectionState.Connected)
            {
                var connectionResult = await provider.ConnectAsync();
                return connectionResult;
            }

            return new MetaConnectionResult()
            {
                Success = true,
                Error = string.Empty,
                State = ConnectionState.Connected,
            };
        }
        
        public MetaDataResult RegisterRemoteResult(MetaContractData contractData, RemoteMetaResult response)
        {
            var remoteId = contractData.contractName;
            var contract = contractData.contract;
            var metaData = contractData.metaData;

            var responceData = response.data ?? string.Empty;
            var unixTime = DateTime.Now.ToUnixTimestamp();
            var outputType = contract.OutputType;
            
            outputType = outputType == null || outputType == typeof(VoidRemoteData) 
                ? typeof(string)
                : outputType;
            
            var resultObject = responceData;
            
            switch (outputType)
            {
                case not null when outputType == typeof(string):
                    resultObject = responceData;
                    break;
                case not null when outputType == typeof(VoidRemoteData):
                    resultObject = VoidRemoteData.Empty;
                    break;
            }
            
            var result = new MetaDataResult()
            {
                id = metaData.id,
                payload = contract?.Payload,
                resultType = outputType,
                model = resultObject,
                result = responceData,
                success = response.success,
                hash = responceData.GetHashCode(),
                error = response.error,
                timestamp = unixTime,
            };
                
            if (!response.success)
            {
                Debug.LogError($"Remote Meta Service: remote: {remoteId} payload {contract?.GetType().Name} | error: {response.error}");
            }
            
            return result;
        }
        
        
        private void InitializeCache()
        {
            var items = _metaDataConfiguration.RemoteMetaData;
            foreach (var metaData in items)
            {
                AddRemoteMetaCache(metaData);
            }
        }

        private RemoteMetaData CreateNewRemoteMeta(string methodName)
        {
            var contract = new SimpleMetaCallContract<string, string>();
            var id = _metaDataConfiguration.CalculateMetaId(contract);
            
            return new RemoteMetaData()
            {
                id = id,
                method = methodName,
                contract = contract,
            };
        }

        private bool AddRemoteMetaCache(RemoteMetaData metaCallData)
        {
            if(_metaIdCache.TryGetValue((RemoteMetaId)metaCallData.id,out var _))
                return false;

            _metaIdCache[(RemoteMetaId)metaCallData.id] = metaCallData;

            return true;
        }
    }
}