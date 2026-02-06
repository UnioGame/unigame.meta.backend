namespace UniGame.MetaBackend.Shared
{
    using System;
    using System.Threading;
    using Core.Runtime;
    using Cysharp.Threading.Tasks;
    using MetaService.Runtime;
    using R3;
    using Runtime;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.Rx;

    [Serializable]
    public abstract class RemoteMetaProvider :
        IMetaConnection,
        ILifeTimeContext,
        IRemoteMetaProvider
    {
        public LifeTime lifeTime = new ();
        
        public ReactiveValue<ConnectionState> connectionState =
            new (ConnectionState.Disconnected);
        
        
        public ReadOnlyReactiveProperty<ConnectionState> State => connectionState;
        
        
        public ILifeTime LifeTime => lifeTime;


        public void Dispose() => lifeTime.Terminate();
        

        public async UniTask<MetaConnectionResult> ConnectAsync()
        {
            var result = await ConnectInternalAsync();
            if (result.Success == false)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"MetaProvider {this.GetType().Name} connection failed: {result.Error} | state : {connectionState.CurrentValue}");
#endif
                return result;
            }
            
            connectionState.Value = result.State;
            
            return result;
        }

        public async UniTask DisconnectAsync()
        {
            var result = await DisconnectInternalAsync();
            if (result.Success == false)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"MetaProvider {this.GetType().Name} connection failed: {result.Error} | state : {connectionState.CurrentValue}");
#endif
                return;
            }
            
            connectionState.Value = result.State;
        }

        public abstract bool IsContractSupported(IRemoteMetaContract command);

        public abstract UniTask<ContractMetaResult> ExecuteAsync(MetaContractData contractData,
            CancellationToken cancellationToken = default);

        public abstract bool TryDequeue(out ContractMetaResult result);

        protected abstract UniTask<MetaConnectionResult> ConnectInternalAsync();
        
        protected abstract UniTask<MetaConnectionResult> DisconnectInternalAsync();
    }
}