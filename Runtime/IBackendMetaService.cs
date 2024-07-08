namespace MetaService.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Data;
    using DefaultNamespace;
    using Game.Modules.ModelMapping;
    using UniGame.Core.Runtime;

    public interface IBackendMetaService : 
        IMetaConnection,
        IRemoteMetaMatchmaking,
        ILifeTimeContext
    {
        IRemoteMetaDataConfiguration MetaDataConfiguration { get; }
        
        IObservable<MetaDataResult> DataStream { get; }
        
        void SwitchProvider(int providerId);
        
        UniTask<MetaDataResult> InvokeAsync(object payload);
        
        UniTask<MetaDataResult> InvokeAsync<TContract>(TContract payload)
            where TContract : IRemoteMetaCall;

        UniTask<MetaDataResult> InvokeAsync(int remoteId, object payload);
        
        UniTask<MetaDataResult> InvokeAsync(string remoteId, string payload);
        
        UniTask<MetaDataResult> InvokeAsync(Type resultType, object payload);

        
        event Action<int, string> OnBackendNotification;
    }

}