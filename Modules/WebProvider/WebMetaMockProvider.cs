namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
    public class WebMetaMockProvider : RemoteMetaProvider
    {
        public const string NotSupportedError = "Not supported";
        
        private WebMetaProviderSettings _settings;
        private Dictionary<Type, WebApiEndPoint> _contractsMap;
        private LifeTime _lifeTime = new();

        public WebMetaMockProvider(WebMetaProviderSettings settings)
        {
            _settings = settings;
            _contractsMap = settings.contracts
                .ToDictionary(x => (Type)x.contract);
        }

        public override bool IsContractSupported(IRemoteMetaContract command)
        {
            return _contractsMap.ContainsKey(command.GetType());
        }

        public async UniTask<ContractMetaResult> ExecuteAsync(
            IRemoteMetaContract contract, 
            CancellationToken cancellationToken = default)
        {
            var contractType = contract.GetType();
            var result = new ContractMetaResult()
            {
                error = NotSupportedError,
                data = null,
                success = true,
                id = contractType.Name,
            };
            
            if (!_contractsMap.TryGetValue(contractType, out var endPoint))
                return result;

            var debugResult = endPoint.debugResult;
            result = new ContractMetaResult()
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

        
        public override async UniTask<ContractMetaResult> ExecuteAsync(MetaContractData data,
            CancellationToken cancellationToken = default)
        {
            var result = await ExecuteAsync(data.contract,cancellationToken);
            result.id = data.contractName;
            return result;
        }

        public override bool TryDequeue(out ContractMetaResult result)
        {
            result = default;
            return false;
        }

        protected override async UniTask<MetaConnectionResult> ConnectInternalAsync()
        {
            return new MetaConnectionResult()
            {
                Error = string.Empty,
                Success = true,
                State = ConnectionState.Connected,
            };
        }

        protected override async UniTask<MetaConnectionResult> DisconnectInternalAsync()
        {
            return new MetaConnectionResult()
            {
                State = ConnectionState.Disconnected,
                Success = true,
                Error = string.Empty,
            };
        }

    }
}