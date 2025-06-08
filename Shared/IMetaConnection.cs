namespace UniGame.MetaBackend.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Data;
    using R3;


    public interface IMetaConnection : IDisposable
    {
        ReadOnlyReactiveProperty<ConnectionState> State { get; }
        
        UniTask<MetaConnectionResult> ConnectAsync();
        
        UniTask DisconnectAsync();
    }
}