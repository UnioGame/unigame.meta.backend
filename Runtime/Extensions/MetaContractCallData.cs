namespace Extensions
{
    using System.Threading;
    using UniGame.MetaBackend.Shared;

    public struct MetaContractCallData
    {
        public IRemoteMetaContract Contract;
        public CancellationToken CancellationToken;
    }
}