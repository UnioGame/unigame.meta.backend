namespace MetaService.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Data;
    using UniGame.Core.Runtime;

    public interface IRemoteMetaProvider:
        IMetaConnection,
        IRemoteMetaMatchmaking,
        IDisposable,
        ILifeTimeContext
    {
        UniTask<RemoteMetaResult> CallRemoteAsync(string method,string data);
        event Action<MetaNotificationResult> OnBackendNotification;
    }
}