namespace MetaService.Runtime
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DefaultNamespace;
    using Game.Modules.ModelMapping;
    using ModelMapping;
    using Newtonsoft.Json;
    using Shared;
    using Shared.Data;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.UniNodes.GameFlow.Runtime;
    using UniModules.UniCore.Runtime.DateTime;
    using UniRx;
    using UnityEngine;

    [Serializable]
    public class BackendMetaService : GameService, IBackendMetaService
    {
        private IRemoteMetaDataConfiguration _metaDataConfiguration;
        private IRemoteMetaProvider _defaultMetaProvider;
        private readonly IDictionary<int, IRemoteMetaProvider> _metaProviders;
        private Dictionary<int,MetaDataResult> _responceCache;
        private Dictionary<int,RemoteMetaCallData> _metaIdCache;
        private Dictionary<string,RemoteMetaCallData> _metaMethodCache;
        private Dictionary<Type,RemoteMetaCallData> _resultTypeCache;
        private Subject<MetaDataResult> _dataStream;
        private string _connectionId = string.Empty;
        
        public event Action<MetaNotification, MetaDataResult> OnBackendNotification;

        public BackendMetaService(IRemoteMetaProvider defaultMetaProvider,
            IDictionary<int,IRemoteMetaProvider> metaProviders,
            IRemoteMetaDataConfiguration metaDataConfiguration)
        {
            _responceCache = new Dictionary<int, MetaDataResult>();
            _metaIdCache = new Dictionary<int, RemoteMetaCallData>();
            _resultTypeCache = new Dictionary<Type, RemoteMetaCallData>();
            _metaMethodCache = new Dictionary<string, RemoteMetaCallData>();
            _dataStream = new Subject<MetaDataResult>().AddTo(LifeTime);
            
            _metaDataConfiguration = metaDataConfiguration;
            _defaultMetaProvider = defaultMetaProvider;
            _metaProviders = metaProviders;

            _defaultMetaProvider.OnBackendNotification += ProviderNotification_Callback;
            
            InitializeCache();
        }

        public IObservable<MetaDataResult> DataStream => _dataStream;

        public IReadOnlyReactiveProperty<ConnectionState> State => _defaultMetaProvider.State;
        
        public IRemoteMetaDataConfiguration MetaDataConfiguration => _metaDataConfiguration;
        
        public IRemoteMetaProvider GetProvider(int providerId)
        {
            if (_metaProviders.TryGetValue(providerId, out var provider))
                return provider;
            return _defaultMetaProvider;
        }        
        
        public async UniTask<MetaConnectionResult> ConnectAsync(string connectionId)
        {
            _connectionId = connectionId;
#if UNITY_EDITOR
            Debug.Log($"BackendMetaService ConnectAsync with deviceId: {_connectionId}");
#endif
            return await _defaultMetaProvider.ConnectAsync(_connectionId);
        }

        public async UniTask DisconnectAsync()
        {
            await _defaultMetaProvider.DisconnectAsync();
        }

        public void SwitchProvider(int providerId)
        {
            _defaultMetaProvider = GetProvider(providerId);
        }

        public async UniTask<MetaDataResult> InvokeAsync(object payload)
        {
            var type = payload.GetType();
            var result = await InvokeAsync(type,payload);
            return result;
        }

        public async UniTask<MetaDataResult> InvokeAsync<TContract>(TContract contract)
            where TContract : IRemoteMetaCall
        {
            var meta = FindMetaData(contract);
            if (meta == RemoteMetaCallData.Empty)
                return MetaDataResult.Empty;
            return await InvokeAsync(meta.id,contract.Payload);
        }
        
        public async UniTask<MetaDataResult> InvokeAsync(string remoteId, string payload)
        {
            return await InvokeAsync(_defaultMetaProvider,remoteId, payload);
        }
        
        public async UniTask<MetaDataResult> InvokeAsync(int remoteId,object payload)
        {
            var metaData = FindMetaData(remoteId);
            if (metaData == RemoteMetaCallData.Empty)
                return new MetaDataResult();
            
            var result = await InvokeAsync(metaData, payload);
            return result;
        }
        
        public async UniTask<MetaDataResult> InvokeAsync(Type resultType,object payload)
        {
            var metaData = FindMetaData(resultType);
            if (metaData == RemoteMetaCallData.Empty)
                return new MetaDataResult();
            
            var result = await InvokeAsync(metaData, payload);
            return result;
        }

        private async UniTask<MetaDataResult> InvokeAsync(IRemoteMetaProvider provider,string remoteId, string payload)
        {
            try
            {
                payload = string.IsNullOrEmpty(payload) ? string.Empty : payload;

                if (provider.State.Value != ConnectionState.Connected)
                {
                    var connectionResult = await provider.ConnectAsync(_connectionId);
                    if(connectionResult.Success == false)
                        return MetaDataResult.Empty;
                }
                
                var remoteResult = await provider.CallRemoteAsync(remoteId,payload);

                var result = RegisterRemoteResult(remoteId,payload,remoteResult);

                _responceCache.TryGetValue(result.Id, out var response);
                _responceCache[result.Id] = result;
                
                var isChanged = response == null || response.Hash != result.Hash;
                if(isChanged && result.Success) 
                    _dataStream.OnNext(response);
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return MetaDataResult.Empty;
            }
        }

        private void ProviderNotification_Callback(MetaNotificationResult notificationResult)
        {
            var contractsData = _metaDataConfiguration.RemoteMetaNotificationData;
            IRemoteNotificationContract contract = null;
            IRemoteDataConverter converter = null;
            var notificationId = notificationResult.Code;
            var rawResult = notificationResult.Content;
            var creationTime = notificationResult.CreateTime;
            foreach (var data in contractsData)
            {
                if (data.contract.NotificationId == notificationId)
                {
                    contract = data.contract;
                    converter = data.overriderDataConverter
                        ? data.converter
                        : _metaDataConfiguration.Converter;
                    break;
                }
            }

            if (contract == null || converter == null)
            {
                GameLog.LogWarning($"No notification config for id {notificationId} or config contains invalid parameters");
                return;
            }

            var resultType = contract.DtoType;
            var resultModel = converter.Convert(contract.DtoType, rawResult);
            var timestamp = DateTime.TryParse(creationTime, out var parsedCreationTime)
                ? parsedCreationTime.ToUnixTimestamp()
                : DateTime.Now.ToUnixTimestamp();
            
            MetaDataResult resultData = new MetaDataResult
            {
                Success = true,
                ResultType = resultType,
                Timestamp = timestamp,
                RawResult = rawResult,
                Model = resultModel
            };
            
#if UNITY_EDITOR || DEBUG
            GameLog.Log($"Backend notification: id: {notificationId}\ncontent: {rawResult}");
#endif
            OnBackendNotification?.Invoke(notificationId, resultData);
        }
        
        public MetaDataResult RegisterRemoteResult(
            string remoteId,
            string payload,
            RemoteMetaResult response)
        {
            if(!_metaMethodCache.TryGetValue(remoteId, out var metaData))
            {
                metaData = CreateNewRemoteMeta(remoteId);
                metaData.method = remoteId;
                AddRemoteMetaCache(metaData);
            }

            var responceData = string.IsNullOrEmpty(response.Data) ? 
                string.Empty : response.Data;
            
            var unixTime = DateTime.Now.ToUnixTimestamp();
            var contract = metaData.contract;
            var outputType = contract.OutputType;
            
            outputType = outputType == null || outputType == typeof(VoidRemoteData) 
                ? typeof(string)
                : outputType;
            
            object resultObject = null;
            
            switch (outputType)
            {
                case not null when outputType == typeof(string):
                    resultObject = responceData;
                    break;
                case not null when outputType == typeof(VoidRemoteData):
                    resultObject = VoidRemoteData.Empty;
                    break;
                default:
                    var converter = metaData.overriderDataConverter 
                        ? metaData.converter 
                        : _metaDataConfiguration.Converter;
                    if (converter == null)
                    {
                        Debug.LogError($"Remote Meta Service: remote: {remoteId} payload {payload} | error: converter is null");
                        return MetaDataResult.Empty;
                    }
                    
                    resultObject = converter.Convert(contract.OutputType,responceData);
                    break;
            }
            
            var result = new MetaDataResult()
            {
                Id = metaData.id,
                RawResult = payload,
                ResultType = outputType,
                Model = resultObject,
                Result = responceData,
                Success = response.Success,
                Hash = responceData.GetHashCode(),
                Error = response.Error,
                Timestamp = unixTime,
            };
                
            if (!response.Success)
            {
                Debug.LogError($"Remote Meta Service: remote: {remoteId} payload {payload} | error: {response.Error}");
            }
            
            return result;
        }
        
        private async UniTask<MetaDataResult> InvokeAsync(RemoteMetaCallData metaCallData,object payload)
        {
            var parameter = payload switch
            {
                null => string.Empty,
                string s => s,
                _ => JsonConvert.SerializeObject(payload)
            };
            var provider = GetProvider(metaCallData.provider);
            
            var remoteResult = await InvokeAsync(provider,metaCallData.method, parameter);
            return remoteResult;
        }
        
        public RemoteMetaCallData FindMetaData<TResult>()
        {
            var type = typeof(TResult);
            return FindMetaData(type);
        }
        
        public RemoteMetaCallData FindMetaData<TContract>(TContract contract)
            where TContract : IRemoteMetaCall
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

        public async UniTask<string> AddMatchmakerAsync()
        {
            return await _defaultMetaProvider.AddMatchmakerAsync();
        }
    }
}