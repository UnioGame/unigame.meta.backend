namespace UniGame.MetaBackend.Shared
{
    using System;

    public interface IRemoteMetaContract
    {
        object Payload { get; }
        Type Output { get; }
        Type Input { get; }
    }

    public interface IRemoteMetaContract<TIn,TOut> : IRemoteMetaContract
    {
    }
}