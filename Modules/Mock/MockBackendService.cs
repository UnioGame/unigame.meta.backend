namespace Game.Runtime.Services.Backend.Mock.Data
{
    using System;
    using Cysharp.Threading.Tasks;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using UniGame.Core.Runtime;
    using UniModules.UniCore.Runtime.DataFlow;
    using UniModules.UniGame.Core.Runtime.Rx;
    using UniRx;

    [Serializable]
    public class MockBackendService : IBackendMetaService
    {
        private ReactiveValue<ConnectionState> _connectionState;
        private LifeTimeDefinition _lifeTime;

        public MockBackendService()
        {
            _connectionState = new ReactiveValue<ConnectionState>(ConnectionState.Disconnected);
            _lifeTime = new LifeTimeDefinition();
        }

        public ILifeTime LifeTime => _lifeTime;
        
        public IReadOnlyReactiveProperty<ConnectionState> State => _connectionState;
        
        public UniTask<MetaConnectionResult> ConnectAsync(string deviceId)
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


        public ConnectionState GetConnectionState()
        {
            return _connectionState.Value;
        }

        public void Dispose()
        {
            _lifeTime.Terminate();
            _connectionState.Value = ConnectionState.Closed;
        }

        
    }
}