namespace Game.Runtime.Services.Backend.Mock.Data
{
    using System;
    using Cysharp.Threading.Tasks;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using UniGame.Core.Runtime;
    using UniModules.UniCore.Runtime.DataFlow;

    [Serializable]
    public class MockBackendService : IBackendMetaService
    {
        private ConnectionState _connectionState;
        private LifeTimeDefinition _lifeTime;

        public MockBackendService()
        {
            _connectionState = ConnectionState.Disconnected;
            _lifeTime = new LifeTimeDefinition();
        }

        public ILifeTime LifeTime => _lifeTime;
        
        public ConnectionState State => _connectionState;
        
        public UniTask<MetaConnectionResult> ConnectAsync(string deviceId)
        {
            _connectionState = ConnectionState.Connected;
            var result = new MetaConnectionResult
            {
                Success = true,
                Error = string.Empty,
                State = _connectionState
            };
            return UniTask.FromResult(result);
        }

        public UniTask DisconnectAsync()
        {
            _connectionState = ConnectionState.Disconnected;
            var result = new MetaConnectionResult
            {
                Success = true,
                Error = string.Empty,
                State = _connectionState
            };
            return UniTask.FromResult(result);
        }


        public ConnectionState GetConnectionState()
        {
            return _connectionState;
        }

        public void Dispose()
        {
            _lifeTime.Terminate();
            _connectionState = ConnectionState.Closed;
        }

        
    }
}