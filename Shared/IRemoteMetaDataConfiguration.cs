namespace Game.Modules.ModelMapping
{
    using UniGame.MetaBackend.Shared;

    public interface IRemoteMetaDataConfiguration
    {
        public IRemoteDataConverter Converter { get; }

        public RemoteMetaData[] RemoteMetaData { get; }

        string GetContractName(IRemoteMetaContract contract);
        string GetRemoteMethodName(IRemoteMetaContract contract);
        int CalculateMetaId(IRemoteMetaContract contract);
    }
}