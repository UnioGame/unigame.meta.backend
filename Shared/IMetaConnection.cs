namespace UniGame.MetaBackend.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Runtime;
    using R3;


    public interface IMetaConnection : IDisposable
    {
        public ReadOnlyReactiveProperty<ConnectionState> State { get; }

        public UniTask<MetaConnectionResult> ConnectAsync();

        public UniTask DisconnectAsync();
    }
}