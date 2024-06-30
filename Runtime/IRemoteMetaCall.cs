namespace DefaultNamespace
{
    using System;

    public interface IRemoteMetaCall
    {
        object Payload { get; }
        Type Output { get; }
        Type Input { get; }
    }
}