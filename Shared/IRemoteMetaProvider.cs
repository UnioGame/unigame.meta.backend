namespace MetaService.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Data;
    using UniGame.Core.Runtime;

    public interface IRemoteMetaProvider:
        IMetaConnection,
        IDisposable,
        ILifeTimeContext
    {
        UniTask<RemoteMetaResult> CallRemoteAsync(string method,string data);
    }
}