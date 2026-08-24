namespace UniGame.MetaBackend.Runtime
{
    using Shared;

    public interface INakamaContract : IRemoteMetaContract
    {
        
    }

    /// <summary>
    /// Marks RPC contracts whose request and response payloads must never be logged.
    /// </summary>
    public interface ISensitiveNakamaContract : INakamaContract
    {
    }
}
