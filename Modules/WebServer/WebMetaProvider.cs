namespace Modules.WebServer
{
    using System;
    using Cysharp.Threading.Tasks;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using UniGame.Core.Runtime;
    using UniModules.UniCore.Runtime.DataFlow;
    using UniRx;

    [Serializable]
    public class WebMetaProvider : IRemoteMetaProvider
    {
        private LifeTime _lifeTime;

        public WebMetaProvider()
        {
            _lifeTime = new LifeTime();
        }
        
        public ILifeTime LifeTime => _lifeTime;
        
        public IReadOnlyReactiveProperty<ConnectionState> State { get; }
        
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
        }
    }
}