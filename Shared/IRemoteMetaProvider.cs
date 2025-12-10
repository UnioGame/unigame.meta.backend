namespace UniGame.MetaBackend.Shared
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Runtime;
    using MetaService.Runtime;
    using UniGame.Core.Runtime;

    public interface IRemoteMetaProvider:
        IMetaConnection,
        ILifeTimeContext
    {
        public bool IsContractSupported(IRemoteMetaContract command);

        public UniTask<ContractMetaResult> ExecuteAsync(MetaContractData contractData,CancellationToken cancellationToken = default);

        public bool TryDequeue(out ContractMetaResult result);
    }
}