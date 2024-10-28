namespace Modules.WebServer
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
    public class WebMetaProvider : IRemoteMetaProvider
    {
        private LifeTime _lifeTime = new();
        private ReactiveValue<ConnectionState> _connectionState = new(ConnectionState.Disconnected);
        
        public ILifeTime LifeTime => _lifeTime;

        public IReadOnlyReactiveProperty<ConnectionState> State => _connectionState;
        
        public UniTask DisconnectAsync()
        {
            return UniTask.CompletedTask;
        }

        public async UniTask<MetaConnectionResult> ConnectAsync(string deviceId)
        {
            return default;
        }
        
        public async UniTask<RemoteMetaResult> CallRemoteAsync(string method, string data)
        {
            return default;
        }
        
        public void Dispose()
        {
            _lifeTime.Release();
        }
    }
}