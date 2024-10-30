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
        private Dictionary<int,RemoteMetaCallData> _metaIdCache;
        private Dictionary<string,RemoteMetaCallData> _metaMethodCache;
        private Dictionary<Type,RemoteMetaCallData> _resultTypeCache;
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
            _metaIdCache = new Dictionary<int, RemoteMetaCallData>(64);
            _resultTypeCache = new Dictionary<Type, RemoteMetaCallData>(64);
            _metaMethodCache = new Dictionary<string, RemoteMetaCallData>(64);
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

            if (_metaProviders.TryGetValue(providerId, out var contractProvider))
                return contractProvider;
            
            foreach (var metaProvider in _metaProviders.Values)
            {
                if(!metaProvider.IsContractSupported(data.contract))
                    continue;
                provider = metaProvider;
                break;
            }
                        
            provider ??= _defaultMetaProvider;
            _contractsCache[contractType] = provider;
            
            return provider;
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
            if (meta == RemoteMetaCallData.Empty) 
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
        
                
        public RemoteMetaCallData FindMetaData<TResult>()
        {
            var type = typeof(TResult);
            return FindMetaData(type);
        }
        
        public RemoteMetaCallData FindMetaData<TContract>(TContract contract)
            where TContract : IRemoteMetaContract
        {
            foreach (var meta in _metaDataConfiguration.RemoteMetaData)
            {
                if(meta.contract.OutputType == contract.Output && 
                   meta.contract.InputType == contract.Input)
                    return meta;
            }
            
            return RemoteMetaCallData.Empty;
        }
        
        public RemoteMetaCallData FindMetaData(Type resultType)
        {
            return _resultTypeCache.TryGetValue(resultType, out var metaData) 
                ? metaData : RemoteMetaCallData.Empty;
        }
        
        public RemoteMetaCallData FindMetaData(int metaId)
        {
            if (_metaIdCache.TryGetValue(metaId, out var metaData))
                return metaData;
            return RemoteMetaCallData.Empty;
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
            
#if UNITY_EDITOR
            GameLog.Log($"Backend result: {response.Data}",Color.green);
#endif
            
            if(!_metaMethodCache.TryGetValue(remoteId, out var metaData))
            {
                metaData = CreateNewRemoteMeta(remoteId);
                metaData.method = remoteId;
                AddRemoteMetaCache(metaData);
            }

            var responceData = response.Data ?? string.Empty;
            var unixTime = DateTime.Now.ToUnixTimestamp();
            var outputType = contract.Output;
            
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
                success = response.Success,
                hash = responceData.GetHashCode(),
                error = response.Error,
                timestamp = unixTime,
            };
                
            if (!response.Success)
            {
                Debug.LogError($"Remote Meta Service: remote: {remoteId} payload {contract?.GetType().Name} | error: {response.Error}");
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

        private RemoteMetaCallData CreateNewRemoteMeta(string methodName)
        {
            var contract = new SimpleMetaCallContract<string, string>();
            var id = _metaDataConfiguration.CalculateMetaId(contract);
            
            return new RemoteMetaCallData()
            {
                id = id,
                method = methodName,
                contract = contract,
            };
        }

        private bool AddRemoteMetaCache(RemoteMetaCallData metaCallData)
        {
            if(_metaIdCache.TryGetValue((RemoteMetaId)metaCallData.id,out var _))
                return false;

            var contract = metaCallData.contract;
            
            _metaIdCache.Add((RemoteMetaId)metaCallData.id,metaCallData);
            _resultTypeCache[contract.OutputType] = metaCallData;
            _metaMethodCache[metaCallData.method] = metaCallData;

            return true;
        }
    }
}