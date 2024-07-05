namespace Game.Runtime.Services.Backend.Mock.Data
{
    using System;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using UniGame.Core.Runtime;
    using UniModules.UniCore.Runtime.DataFlow;
    using UniModules.UniGame.Core.Runtime.Rx;
    using UniRx;

    [Serializable]
    public class MockBackendService : IRemoteMetaProvider
    {
        private const string TestTicket = "39eaa676-f22b-45e4-9f6d-571a1632fb3c";
        private const string TestSessionToken = "MBjQoiq7MY3h1lsphDPYpJe3vA4O1lEOwV8IeehbZWstW0Py8uKXHB6bEwGSOKjR";
        
        private MockBackendDataConfig _config;
        private ReactiveValue<ConnectionState> _connectionState;
        private LifeTimeDefinition _lifeTime;
        
        public event Action<int, string> OnBackendNotification;

        public MockBackendService(MockBackendDataConfig config)
        {
            _config = config;
            _connectionState = new ReactiveValue<ConnectionState>(ConnectionState.Disconnected);
            _lifeTime = new LifeTimeDefinition();
        }

        public ILifeTime LifeTime => _lifeTime;
        
        public IReadOnlyReactiveProperty<ConnectionState> State => _connectionState;
        
        public UniTask<MetaConnectionResult> ConnectAsync(string deviceId)
        {
            _connectionState.Value = _config.allowConnect 
                    ? ConnectionState.Connected
                    : ConnectionState.Disconnected;
            
            var result = new MetaConnectionResult
            {
                Success = _config.allowConnect,
                Error = string.Empty,
                State = _connectionState.Value
            };
            
            return UniTask.FromResult(result);
        }

        public UniTask DisconnectAsync()
        {
            _connectionState.Value = ConnectionState.Connected;
            var result = new MetaConnectionResult
            {
                Success = true,
                Error = string.Empty,
                State = _connectionState.Value
            };
            return UniTask.FromResult(result);
        }

        public async UniTask<RemoteMetaResult> CallRemoteAsync(string method, string data)
        {
            var result = _config
                .mockBackendData
                .FirstOrDefault(x => 
                    x.Method.Equals(method, StringComparison.OrdinalIgnoreCase));

            var success = result is { Success: true };
            var resultData = result == null ? string.Empty : result.Result;
            var error = result == null ? string.Empty : result.Error;

            if (method == "AcceptGame")
            {
                OnBackendNotification?.Invoke(1, $"{{\tSessionTicket:\n\t\"{TestSessionToken}\"\t}}");
            }
            
            return new RemoteMetaResult()
            {
                Id = method,
                Error = error,
                Success = success,
                Data = resultData,
            };
        }

        public ConnectionState GetConnectionState()
        {
            return _connectionState.Value;
        }

        public void Dispose()
        {
            _lifeTime.Terminate();
            _connectionState.Value = ConnectionState.Closed;
        }

        public async UniTask<string> AddMatchmakerAsync()
        {
            await UniTask.Yield();
            return TestTicket;
        }
    }
}