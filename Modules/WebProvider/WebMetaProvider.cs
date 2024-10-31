namespace Modules.WebServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using Game.Runtime.Services.WebService;
    using MetaService.Runtime;
    using Newtonsoft.Json;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UniGame.Core.Runtime;
    using UniModules.Runtime.Network;
    using UniModules.UniCore.Runtime.DataFlow;
    using UniModules.UniGame.Core.Runtime.Rx;
    using UniRx;
    using UnityEngine;

    [Serializable]
    public class WebMetaProvider : IWebMetaProvider
    {
        public const string NotSupportedError = "Not supported";

        public static readonly Dictionary<string,string> EmptyQuery = new();
        public static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
        };
        
        private WebMetaProviderSettings _settings;
        private Dictionary<Type, ApiEndPoint> _contractsMap;
        
        private WebRequestBuilder _webRequestBuilder = new();
        private LifeTime _lifeTime = new();
        private ReactiveValue<ConnectionState> _connectionState = new(ConnectionState.Connected);
        
        private string _token;
        private bool _debugMode;

        public WebMetaProvider(WebMetaProviderSettings settings)
        {
            _settings = settings;
            _token = settings.defaultToken;
            _debugMode = settings.debugMode;
            _contractsMap = settings.contracts
                .ToDictionary(x => (Type)x.contract);
            
            _webRequestBuilder = new()
            {
                addVersion = true,
#if UNITY_EDITOR
                userToken = _token,
#endif
            };
        }
        
        public ILifeTime LifeTime => _lifeTime;

        public IReadOnlyReactiveProperty<ConnectionState> State => _connectionState;

        public void SetToken(string token)
        {
            _token = token;
        }
        
        public bool IsContractSupported(IRemoteMetaContract command)
        {
            var contractType = command.GetType();
            var containsKey = _contractsMap.ContainsKey(contractType);
            return containsKey;
        }

        public async UniTask<RemoteMetaResult> ExecuteAsync(IRemoteMetaContract contract)
        {
            var contractType = contract.GetType();
            var result = new RemoteMetaResult()
            {
                error = NotSupportedError,
                data = null,
                success = true,
                Id = contractType.Name,
            };
            
            if (!_contractsMap.TryGetValue(contractType, out var endPoint))
                return result;

            if (_debugMode)
                return ExecuteDebugAsync(contract, endPoint);

            var payload = contract.Payload;
            var url = endPoint.url;
            var token = _token;

            if (contract is IWebRequestContract webRequestContract)
            {
                url = string.IsNullOrEmpty(webRequestContract.Url)
                    ? url
                    : webRequestContract.Url;
                token = string.IsNullOrEmpty(webRequestContract.Token)
                    ? token
                    : webRequestContract.Token;
            }
            
            _webRequestBuilder.SetToken(token);
            
            var serializedPayload = payload == null 
                ? string.Empty 
                : JsonConvert.SerializeObject(payload, JsonSettings);

            GameLog.Log($"{contract.GetType().Name} : {url} : {serializedPayload}",Color.cyan);

            var requestResult = new WebServerResult();
            
            switch (endPoint.requestType)
            {
                case WebRequestType.Post:
                    requestResult = await _webRequestBuilder.PostAsync(url, serializedPayload);
                    break;
                case WebRequestType.Get:
                    var query = SerializeToQuery(payload);
                    requestResult = await _webRequestBuilder.GetAsync(url,query);
                    break;
            }

            var resultData = requestResult.success 
                ? JsonConvert.DeserializeObject(requestResult.data,contract.OutputType) 
                : null;
            
            result.data = resultData;
            result.success = requestResult.success;
            result.error = requestResult.error;
            
            return result;
        }

        public RemoteMetaResult ExecuteDebugAsync(IRemoteMetaContract contract,ApiEndPoint endPoint)
        {
            var debugResult = endPoint.debugResult;
            var result = new RemoteMetaResult()
            {
                error = debugResult.error,
                data = null,
                success = debugResult.success,
            };

            if (!debugResult.success) return result;

            var data = JsonConvert.DeserializeObject(debugResult.data, contract.OutputType);
            result.data = data;
            return result;
        }
        
        public async UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData data)
        {
            var result = await ExecuteAsync(data.contract);
            result.Id = data.contractName;
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