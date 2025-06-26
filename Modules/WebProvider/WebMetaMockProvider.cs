namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using WebService;
    using MetaService.Runtime;
    using Newtonsoft.Json;
    using R3;
    using UniCore.Runtime.ProfilerTools;
    using Shared;
    using UniGame.Core.Runtime;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.Rx;
     
    using UnityEngine;

    [Serializable]
    public class WebMetaMockProvider : IRemoteMetaProvider
    {
        public const string NotSupportedError = "Not supported";

        
        private WebMetaProviderSettings _settings;
        private Dictionary<Type, WebApiEndPoint> _contractsMap;
        
        private LifeTime _lifeTime = new();
        private ReactiveValue<ConnectionState> _connectionState = new(ConnectionState.Connected);

        public WebMetaMockProvider(WebMetaProviderSettings settings)
        {
            _settings = settings;
            _contractsMap = settings.contracts
                .ToDictionary(x => (Type)x.contract);
        }
        
        public ILifeTime LifeTime => _lifeTime;

        public ReadOnlyReactiveProperty<ConnectionState> State => _connectionState;

        public bool IsContractSupported(IRemoteMetaContract command)
        {
            return true;
        }

        public async UniTask<RemoteMetaResult> ExecuteAsync(IRemoteMetaContract contract)
        {
            var contractType = contract.GetType();
            var result = new RemoteMetaResult()
            {
                error = NotSupportedError,
                data = null,
                success = true,
                id = contractType.Name,
            };
            
            if (!_contractsMap.TryGetValue(contractType, out var endPoint))
                return result;

            var debugResult = endPoint.debugResult;
            result = new RemoteMetaResult()
            {
                error = debugResult.error,
                data = null,
                success = debugResult.success,
            };

            if (!debugResult.success) return result;
            
#if UNITY_EDITOR
            if (_settings.enableLogs)
            {
                var color = result.success ? Color.green : Color.red;
                GameLog.Log($"[WebMetaProvider] [{endPoint.requestType}] : {contract.GetType().Name} : {endPoint.url} : {result.data}",color);
            }
#endif
            
            var data = JsonConvert.DeserializeObject(debugResult.data, contract.OutputType);
            result.data = data;
            
            return result;
        }

        
        public async UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData data)
        {
            var result = await ExecuteAsync(data.contract);
            result.id = data.contractName;
            return result;
        }

        public bool TryDequeue(out RemoteMetaResult result)
        {
            result = default;
            return false;
        }

        public async UniTask<MetaConnectionResult> ConnectAsync()
        {
            return new MetaConnectionResult()
            {
                Error = string.Empty,
                Success = true,
                State = ConnectionState.Connected,
            };
        }

        public UniTask DisconnectAsync()
        {
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            _lifeTime.Release();
        }

    }
}