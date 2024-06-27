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
        private Dictionary<int,RemoteMetaData> _metaIdCache;
        private Dictionary<string,RemoteMetaData> _metaMethodCache;
        private Dictionary<Type,RemoteMetaData> _resultTypeCache;
        private Subject<MetaDataResult> _dataStream;
        private int _nextId = -1;

        public BackendMetaService(IRemoteMetaDataConfiguration metaDataConfiguration,IRemoteMetaProvider remoteMetaProvider)
        {
            _responceCache = new Dictionary<int, MetaDataResult>();
            _metaIdCache = new Dictionary<int, RemoteMetaData>();
            _resultTypeCache = new Dictionary<Type, RemoteMetaData>();
            _metaMethodCache = new Dictionary<string, RemoteMetaData>();
            _dataStream = new Subject<MetaDataResult>()
                .AddTo(LifeTime);
            
            _metaDataConfiguration = metaDataConfiguration;
            _remoteMetaProvider = remoteMetaProvider;
            
            InitializeCache();
        }

        public IObservable<MetaDataResult> DataStream => _dataStream;

        public IReadOnlyReactiveProperty<ConnectionState> State => _remoteMetaProvider.State;
        
        public IRemoteMetaDataConfiguration MetaDataConfiguration => _metaDataConfiguration;
        
        public UniTask<MetaDataResult> InvokeAsync(object payload)
        {
            throw new NotImplementedException();
        }
        
        public async UniTask<MetaConnectionResult> ConnectAsync(string deviceId)
        {
            return await _remoteMetaProvider.ConnectAsync(deviceId);
        }

        public async UniTask DisconnectAsync()
        {
            await _remoteMetaProvider.DisconnectAsync();
        }
        
        public RemoteMetaData FindMetaData<TResult>()
        {
            var type = typeof(TResult);
            return FindMetaData(type);
        }
        
        public RemoteMetaData FindMetaData(Type type)
        {
            return _resultTypeCache.TryGetValue(type, out var metaData) 
                ? metaData : RemoteMetaData.Empty;
        }
        
        public RemoteMetaData FindMetaData(RemoteMetaId metaId)
        {
            if (_metaIdCache.TryGetValue(metaId, out var metaData))
                return metaData;
            return RemoteMetaData.Empty;
        }

        public async UniTask<MetaDataResult> InvokeAsync(string remoteId, string payload)
        {
            try
            {
                payload = string.IsNullOrEmpty(payload) ? string.Empty : payload;
                
                var remoteResult = await _remoteMetaProvider.CallRemoteAsync(remoteId,payload);

                var result = await RegisterRemoteResultAsync(remoteId,payload,remoteResult);

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

        public async UniTask<MetaDataResult> RegisterRemoteResultAsync(
            string remoteId,
            string payload,
            RemoteMetaResult response)
        {
            if(!_metaMethodCache.TryGetValue(remoteId, out var metaData))
            {
                metaData = CreateNewRemoteMeta();
                metaData.method = remoteId;
                AddRemoteMetaCache(metaData);
            }

            var responceData = string.IsNullOrEmpty(response.Data) ? 
                string.Empty : response.Data;
            
            var unixTime = DateTime.Now.ToUnixTimestamp();

            var result = new MetaDataResult()
            {
                Id = metaData.id,
                Payload = payload,
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
            if (metaData == RemoteMetaData.Empty)
                return new MetaDataResult();
            
            var result = await InvokeAsync(metaData, payload);
            return result;
        }
        
        public async UniTask<MetaDataResult> InvokeAsync(Type resultType,object payload)
        {
            var metaData = FindMetaData(resultType);
            if (metaData == RemoteMetaData.Empty)
                return new MetaDataResult();
            
            var result = await InvokeAsync(metaData, payload);
            return result;
        }
        
        private async UniTask<MetaDataResult> InvokeAsync(RemoteMetaData metaData,object payload)
        {
            var parameter = payload == null
                ? string.Empty : payload is string s
                    ? s : JsonConvert.SerializeObject(payload);
            
            var remoteResult = await InvokeAsync(metaData.method, parameter);
            return remoteResult;
        }

        private void InitializeCache()
        {
            var items = _metaDataConfiguration.RemoteMetaData;
            foreach (var metaData in items)
            {
                AddRemoteMetaCache(metaData);
            }
        }

        private RemoteMetaData CreateNewRemoteMeta()
        {
            _nextId++;

            return new RemoteMetaData()
            {
                id = _nextId,
                result = typeof(string),
                method = string.Empty,
                parameter = typeof(string),
                name = _nextId.ToStringFromCache(),
            };
        }

        private bool AddRemoteMetaCache(RemoteMetaData metaData)
        {
            if(_metaIdCache.TryGetValue((RemoteMetaId)metaData.id,out var _))
                return false;
            
            if(metaData.id > _nextId) _nextId = metaData.id;
                
            _metaIdCache.Add((RemoteMetaId)metaData.id,metaData);
            _resultTypeCache.Add(metaData.result,metaData);
            _metaMethodCache.Add(metaData.method,metaData);

            return true;
        }
    }

}