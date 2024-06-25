namespace MetaService.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using MetaService.Shared.Data;
    using UniGame.Core.Runtime.Rx;
    using UniRx;

    public interface IMetaConnection : IDisposable
    {
        IReadOnlyReactiveProperty<ConnectionState> State { get; }
        
        UniTask<MetaConnectionResult> ConnectAsync(string deviceId);
        
        UniTask DisconnectAsync();
    }
}