namespace Game.Modules.ModelMapping
{
    using UniGame.MetaBackend.Shared;

    public interface IRemoteMetaDataConfiguration
    {
        public IRemoteDataConverter Converter { get; }

        public RemoteMetaCallData[] RemoteMetaData { get; }

        string GetContractName(IRemoteCallContract contract);
        string GetRemoteMethodName(IRemoteCallContract contract);
        int CalculateMetaId(IRemoteCallContract contract);
    }
}