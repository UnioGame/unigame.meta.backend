namespace UniGame.MetaBackend.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Data;
    using MetaService.Runtime;
    using UniGame.Core.Runtime;

    public interface IRemoteMetaProvider:
        IMetaConnection,
        IDisposable,
        ILifeTimeContext
    {
        bool IsContractSupported(IRemoteMetaContract command);
        
        UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData contractData);
    }
}