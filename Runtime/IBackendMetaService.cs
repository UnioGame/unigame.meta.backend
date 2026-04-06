namespace UniGame.MetaBackend.Shared
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Runtime;
    using Game.Modules.ModelMapping;
    using global::Shared;
    using R3;
    using UniGame.Core.Runtime;

    public interface IBackendMetaService :
        ILifeTimeContext
    {
        IRemoteMetaDataConfiguration MetaDataConfiguration { get; }
        Observable<ContractDataResult> DataStream { get; }
        
        UniTask<ContractDataResult> ExecuteAsync(IRemoteMetaContract payload, CancellationToken cancellationToken = default);
        
        public bool AddContractHandler(IMetaContractHandler handler);
        public bool RemoveContractHandler<T>() where T : IMetaContractHandler;
        void SwitchProvider(int providerId);
        IRemoteMetaProvider GetProvider(int id);
    }
}