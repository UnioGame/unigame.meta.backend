namespace Modules.WebServer
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using MetaService.Runtime;
    using Newtonsoft.Json;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UniGame.Core.Runtime;
    using UniModules.Runtime.Network;
    using UniModules.UniCore.Runtime.DataFlow;
    using UniModules.UniGame.Core.Runtime.Rx;
    using UniRx;

    [Serializable]
    public class WebMetaProvider : IRemoteMetaProvider
    {
        public const string NotSupportedError = "Not supported";

        public static readonly Dictionary<string,string> EmptyQuery = new();
        public static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
        };
        
        private WebRequestBuilder _webRequestBuilder = new();
        private LifeTime _lifeTime = new();
        private ReactiveValue<ConnectionState> _connectionState = new(ConnectionState.Connected);
        
        public ILifeTime LifeTime => _lifeTime;

        public IReadOnlyReactiveProperty<ConnectionState> State => _connectionState;

        
        public bool IsContractSupported(IRemoteMetaContract command)
        {
            return command is IWebRequestContract;
        }

        public async UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData contractData)
        {
            var contract = contractData.contract;
            if(contract is IWebRequestContract webRequestContract)
                return await ExecuteWebAsync(contractData,webRequestContract);
            
            return new RemoteMetaResult()
            {
                success = false,
                data = null,
                error = NotSupportedError,
                Id = contractData.contractName
            };
        }
        
        public async UniTask<RemoteMetaResult> ExecuteWebAsync(MetaContractData data,IWebRequestContract contract)
        {
            var result = new RemoteMetaResult()
            {
                error = NotSupportedError,
                data = null,
                success = true,
                Id = data.contractName,
            };
            
            if(!string.IsNullOrEmpty(contract.Token))
                _webRequestBuilder.SetToken(contract.Token);

            var payload = contract.Payload;
            var url = contract.Url;
            
            var serializedPayload = payload == null 
                ? string.Empty 
                : JsonConvert.SerializeObject(payload, JsonSettings);

            var requestResult = new WebServerResult
            {
                success = false,
                data = string.Empty,
                exception = null,
                error = string.Empty,
            };
            
            switch (contract)
            {
                case IPostRequestContract postRequestContract:
                    requestResult = await _webRequestBuilder.PostAsync(url, serializedPayload);
                    break;
                case IGetRequestContract getRequestContract:
                    var query = SerializeToQuery(payload);
                    requestResult = await _webRequestBuilder.GetAsync(url,query);
                    break;
            }

            var resultData = requestResult.success 
                ? JsonConvert.DeserializeObject(requestResult.data,contract.Output) 
                : null;
            
            result.data = resultData;
            result.success = requestResult.success;
            result.error = requestResult.error;
            
            return result;
        }

        public Dictionary<string, string> SerializeToQuery(object payload)
        {
            if (payload == null) return EmptyQuery;
            var json = JsonConvert.SerializeObject(payload, JsonSettings);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
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