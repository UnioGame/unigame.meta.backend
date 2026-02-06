namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using MetaService.Runtime;
    using R3;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UniGame.Core.Runtime;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.Rx;
     

    [Serializable]
    public class MockBackendService : RemoteMetaProvider
    {
        private MockBackendDataConfig _config;
        private ReactiveValue<ConnectionState> _connectionState;
        private LifeTime _lifeTime;
        private HashSet<string> _mockedMethods = new();

        public MockBackendService(MockBackendDataConfig config)
        {
            _config = config;
            _connectionState = new ReactiveValue<ConnectionState>(ConnectionState.Disconnected);
            _lifeTime = new LifeTime();

            foreach (var mockBackendData in _config.mockBackendData)
            {
                _mockedMethods.Add(mockBackendData.Method);
            }
        }

        protected override UniTask<MetaConnectionResult> ConnectInternalAsync()
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

        protected override UniTask<MetaConnectionResult> DisconnectInternalAsync()
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


        public override UniTask<ContractMetaResult> ExecuteAsync(MetaContractData contract, CancellationToken cancellationToken = default)
        {
            var method = contract.contractName;
            var result = _config
                .mockBackendData
                .FirstOrDefault(x => 
                    x.Method.Equals(method, StringComparison.OrdinalIgnoreCase));

            var success = result is { Success: true };
            var resultData = result == null ? string.Empty : result.Result;
            var error = result == null ? string.Empty : result.Error;
            
            var resultValue = new ContractMetaResult()
            {
                id = method,
                error = error,
                success = success,
                data = resultData,
            };

            return UniTask.FromResult(resultValue);
        }

        public override bool TryDequeue(out ContractMetaResult result)
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

        public override bool IsContractSupported(IRemoteMetaContract command)
        {
            return _mockedMethods.Contains(command.Path);
        }
    }
}