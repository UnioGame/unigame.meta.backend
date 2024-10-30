namespace Shared
{
    using UniGame.MetaBackend.Shared;

    public interface IMetaContractHandler
    {
        bool IsValidContract(IRemoteMetaContract contract);
        
        IRemoteMetaContract UpdateContract(IRemoteMetaContract contract);
    }
}