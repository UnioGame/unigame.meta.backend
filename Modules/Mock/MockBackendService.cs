namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using MetaService.Runtime;
    using R3;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UniGame.Core.Runtime;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.Rx;
     

    [Serializable]
    public class MockBackendService : IRemoteMetaProvider
    {
        private MockBackendDataConfig _config;
        private ReactiveValue<ConnectionState> _connectionState;
        private LifeTime _lifeTime;

        public MockBackendService(MockBackendDataConfig config)
        {
            _config = config;
            _connectionState = new ReactiveValue<ConnectionState>(ConnectionState.Disconnected);
            _lifeTime = new LifeTime();
        }

        public ILifeTime LifeTime => _lifeTime;
        
        public ReadOnlyReactiveProperty<ConnectionState> State => _connectionState;
        
        public UniTask<MetaConnectionResult> ConnectAsync()
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

        public UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData contract)
        {
            var method = contract.contractName;
            var result = _config
                .mockBackendData
                .FirstOrDefault(x => 
                    x.Method.Equals(method, StringComparison.OrdinalIgnoreCase));

            var success = result is { Success: true };
            var resultData = result == null ? string.Empty : result.Result;
            var error = result == null ? string.Empty : result.Error;
            
            var resultValue = new RemoteMetaResult()
            {
                id = method,
                error = error,
                success = success,
                data = resultData,
            };

            return UniTask.FromResult(resultValue);
        }

        public bool TryDequeue(out RemoteMetaResult result)
        {
            result = default;
            return false;
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

        public bool IsContractSupported(IRemoteMetaContract command)
        {
            return true;
        }
    }
}