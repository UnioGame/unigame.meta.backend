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
        bool IsContractSupported(IRemoteMetaContract command);
        
        UniTask<ContractMetaResult> ExecuteAsync(MetaContractData contractData,CancellationToken cancellationToken = default);
        
        bool TryDequeue(out ContractMetaResult result);
    }
}