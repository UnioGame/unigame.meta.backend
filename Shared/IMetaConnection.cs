namespace UniGame.MetaBackend.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Data;
    using UniRx;

    public interface IMetaConnection : IDisposable
    {
        IReadOnlyReactiveProperty<ConnectionState> State { get; }
        
        UniTask<MetaConnectionResult> ConnectAsync();
        
        UniTask DisconnectAsync();
    }
}