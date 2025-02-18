namespace Modules.WebServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
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
    using UnityEngine.Serialization;

    [Serializable]
    public class WebMetaProvider : IWebMetaProvider
    {
        public const string NotSupportedError = "Not supported";

        public static Regex UrlPatternRegex = new Regex("{([a-zA-Z0-9_]+)}");

        public static readonly Dictionary<string,string> EmptyQuery = new();
        public static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
        };
        
        private BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance 
                                                                 | BindingFlags.NonPublic 
                                                                 | BindingFlags.IgnoreCase;

        
        private WebMetaProviderSettings _settings;
        private string _defaultUrl;
        private Dictionary<Type, WebApiEndPoint> _contractsMap;
        
        private WebRequestBuilder _webRequestBuilder = new();
        private LifeTime _lifeTime = new();
        private ReactiveValue<ConnectionState> _connectionState = new(ConnectionState.Connected);
        
        private string _token;
        private bool _debugMode;

        public WebMetaProvider(WebMetaProviderSettings settings)
        {
            _settings = settings;
            _defaultUrl = settings.defaultUrl;
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
            _webRequestBuilder.SetToken(token);
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
                id = contractType.Name,
            };
            
            if (!_contractsMap.TryGetValue(contractType, out var endPoint))
                return result;

            var requestResult  = _debugMode 
                ? ExecuteDebugAsync(endPoint) 
                : await ExecuteWebRequest(contract, endPoint);
            
#if UNITY_EDITOR
            if (_settings.enableLogs)
            {
                var color = requestResult.success ? Color.green : Color.red;
                GameLog.Log($"[WebMetaProvider] {contract.GetType().Name} {endPoint.url} result : {requestResult.data} {requestResult.error}",color);
            }
#endif

            var resultData = requestResult.success 
                ? JsonConvert.DeserializeObject(requestResult.data,contract.OutputType) 
                : null;
            
            result.data = resultData;
            result.success = requestResult.success;
            result.error = requestResult.error;
            
            return result;
        }

        public async UniTask<WebServerResult> ExecuteWebRequest(IRemoteMetaContract contract,
            WebApiEndPoint endPoint)
        {
            var payload = contract.Payload;
            var url = string.IsNullOrEmpty(endPoint.url) 
                ? _defaultUrl : endPoint.url;
            url = _webRequestBuilder.GetServerUrl(url, endPoint.path);
            
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

#if UNITY_EDITOR
            if (_settings.enableLogs)
            {
                var color = Color.green;
                GameLog.Log($"[WebMetaProvider] request [{endPoint.requestType}] : {contract.GetType().Name} : {endPoint.url} : {serializedPayload}",color);
            }
#endif
            
            url = UpdateUrlPattern(contract,url);
            
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

            return requestResult;
        }

        public string UpdateUrlPattern(object source,string url)
        {
            var matches = UrlPatternRegex.Matches(url);
            var result = url;
            
            foreach (Match match in matches)
            {
                result = UpdatePattern(source,match, result);
            }

            return result;
        }
        
        public string UpdatePattern(object source,Match match,string url)
        {
            var matchValue = match.Value;
            var thisType = source.GetType();
            var result = url;
            if(match.Groups.Count < 2) return result;
        
            var group = match.Groups[1];
        
            var value = group.Value;
            var field = thisType.GetField(value, _bindingFlags);
            var replaceValue = string.Empty;
            
            if (field != null)
            {
                var fieldValue = field.GetValue(this);
                if (fieldValue != null)
                    replaceValue = fieldValue.ToString();
            }
            
            var property = thisType.GetProperty(value, _bindingFlags);
            if (property != null)
            {
                var propertyValue = property.GetValue(this);
                if (propertyValue != null)
                    replaceValue = propertyValue.ToString();
            }
            
            result = result.Replace(matchValue, replaceValue);

            return result;
        }
        
        public WebServerResult ExecuteDebugAsync(WebApiEndPoint endPoint)
        {
            var debugResult = endPoint.debugResult;
            var result = new WebServerResult()
            {
                error = debugResult.error,
                data = string.Empty,
                success = debugResult.success,
            };

            if (!debugResult.success) return result;

            result.data = debugResult.data;
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