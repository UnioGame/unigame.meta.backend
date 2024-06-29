namespace MetaService.Runtime
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Game.Modules.ModelMapping;
    using Newtonsoft.Json;
    using Shared;
    using Shared.Data;
    using UniGame.UniNodes.GameFlow.Runtime;
    using UniModules.UniCore.Runtime.DateTime;
    using UniModules.UniCore.Runtime.Utils;
    using UniRx;
    using UnityEngine;

    [Serializable]
    public class BackendMetaService : GameService,IBackendMetaService
    {
        private IRemoteMetaDataConfiguration _metaDataConfiguration;
        private IRemoteMetaProvider _remoteMetaProvider;
        private Dictionary<int,MetaDataResult> _responceCache;
        private Dictionary<int,RemoteMetaCallData> _metaIdCache;
        private Dictionary<string,RemoteMetaCallData> _metaMethodCache;
        private Dictionary<Type,RemoteMetaCallData> _resultTypeCache;
        private Subject<MetaDataResult> _dataStream;

        public BackendMetaService(IRemoteMetaDataConfiguration metaDataConfiguration,IRemoteMetaProvider remoteMetaProvider)
        {
            _responceCache = new Dictionary<int, MetaDataResult>();
            _metaIdCache = new Dictionary<int, RemoteMetaCallData>();
            _resultTypeCache = new Dictionary<Type, RemoteMetaCallData>();
            _metaMethodCache = new Dictionary<string, RemoteMetaCallData>();
            _dataStream = new Subject<MetaDataResult>()
                .AddTo(LifeTime);
            
            _metaDataConfiguration = metaDataConfiguration;
            _remoteMetaProvider = remoteMetaProvider;
            
            InitializeCache();
        }

        public IObservable<MetaDataResult> DataStream => _dataStream;

        public IReadOnlyReactiveProperty<ConnectionState> State => _remoteMetaProvider.State;
        
        public IRemoteMetaDataConfiguration MetaDataConfiguration => _metaDataConfiguration;
        
                
        public async UniTask<MetaConnectionResult> ConnectAsync(string deviceId)
        {
#if UNITY_EDITOR
            Debug.Log($"BackendMetaService ConnectAsync with deviceId: {deviceId}");
#endif
            return await _remoteMetaProvider.ConnectAsync(deviceId);
        }

        public async UniTask DisconnectAsync()
        {
            await _remoteMetaProvider.DisconnectAsync();
        }

        
        public UniTask<MetaDataResult> InvokeAsync(object payload)
        {
            throw new NotImplementedException();
        }


        public async UniTask<MetaDataResult> InvokeAsync(string remoteId, string payload)
        {
            try
            {
                payload = string.IsNullOrEmpty(payload) ? string.Empty : payload;
                
                var remoteResult = await _remoteMetaProvider.CallRemoteAsync(remoteId,payload);

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
            
            var resultObject = outputType == typeof(string)
                ? responceData
                : _metaDataConfiguration
                    .Converter
                    .Convert(contract.OutputType,payload);
            
            var result = new MetaDataResult()
            {
                Id = metaData.id,
                Payload = payload,
                ResultType = outputType,
                Model = resultObject,
                Result = response.Data,
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
        
        public async UniTask<MetaDataResult> InvokeAsync(RemoteMetaId remoteId,object payload)
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
        
        private async UniTask<MetaDataResult> InvokeAsync(RemoteMetaCallData metaCallData,object payload)
        {
            var parameter = payload == null
                ? string.Empty : payload is string s
                    ? s : JsonConvert.SerializeObject(payload);
            
            var remoteResult = await InvokeAsync(metaCallData.method, parameter);
            return remoteResult;
        }
        
        public RemoteMetaCallData FindMetaData<TResult>()
        {
            var type = typeof(TResult);
            return FindMetaData(type);
        }
        
        public RemoteMetaCallData FindMetaData(Type type)
        {
            return _resultTypeCache.TryGetValue(type, out var metaData) 
                ? metaData : RemoteMetaCallData.Empty;
        }
        
        public RemoteMetaCallData FindMetaData(RemoteMetaId metaId)
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
            var id = _metaDataConfiguration.CalculateMetaId(methodName, contract);
            
            return new RemoteMetaCallData()
            {
                id = id,
                method = methodName,
                contract = contract,
                name = methodName,
            };
        }

        private bool AddRemoteMetaCache(RemoteMetaCallData metaCallData)
        {
            if(_metaIdCache.TryGetValue((RemoteMetaId)metaCallData.id,out var _))
                return false;

            var contract = metaCallData.contract;
            
            _metaIdCache.Add((RemoteMetaId)metaCallData.id,metaCallData);
            _resultTypeCache.Add(contract.OutputType,metaCallData);
            _metaMethodCache.Add(metaCallData.method,metaCallData);

            return true;
        }
    }

}