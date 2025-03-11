namespace UniGame.MetaBackend.Shared
{
    using Cysharp.Threading.Tasks;
    using Data;
    using MetaService.Runtime;
    using UniGame.Core.Runtime;

    public interface IRemoteMetaProvider:
        IMetaConnection,
        ILifeTimeContext
    {
        bool IsContractSupported(IRemoteMetaContract command);
        
        UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData contractData);
        
        bool TryDequeue(out RemoteMetaResult result);
    }
}