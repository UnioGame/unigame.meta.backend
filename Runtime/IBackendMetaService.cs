namespace UniGame.MetaBackend.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Runtime;
    using Game.Modules.ModelMapping;
    using global::Shared;
    using R3;
    using UniGame.Core.Runtime;

    public interface IBackendMetaService : 
        IMetaConnection,
        ILifeTimeContext
    {
        public bool AddContractHandler(IMetaContractHandler handler);
        public bool RemoveContractHandler<T>() where T : IMetaContractHandler;
        IRemoteMetaDataConfiguration MetaDataConfiguration { get; }
        Observable<MetaDataResult> DataStream { get; }
        void SwitchProvider(int providerId);
        UniTask<MetaDataResult> ExecuteAsync(IRemoteMetaContract payload);
        bool TryDequeueMetaRequest(IRemoteMetaContract contract, out MetaDataResult result);
        IRemoteMetaProvider GetProvider(int id);
        RemoteMetaData FindMetaData(IRemoteMetaContract contract);
    }
}