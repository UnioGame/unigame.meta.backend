namespace UniGame.MetaBackend.Shared
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
        
        void SwitchProvider(int providerId);
        
        UniTask<MetaDataResult> ExecuteAsync(IRemoteMetaContract payload);
    }

}