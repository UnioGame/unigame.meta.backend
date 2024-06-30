namespace MetaService.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Data;
    using Game.Modules.ModelMapping;
    using UniGame.Core.Runtime;

    public interface IBackendMetaService : 
        IMetaConnection,
        ILifeTimeContext
    {
        IRemoteMetaDataConfiguration MetaDataConfiguration { get; }
        
        IObservable<MetaDataResult> DataStream { get; }
        
        UniTask<MetaDataResult> InvokeAsync(object payload);

        UniTask<MetaDataResult> InvokeAsync(RemoteMetaId remoteId, object payload);
        
        UniTask<MetaDataResult> InvokeAsync(string remoteId, string payload);
        
        UniTask<MetaDataResult> InvokeAsync(Type resultType, object payload);
    }
}