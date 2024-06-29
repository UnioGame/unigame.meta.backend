namespace Game.Modules.ModelMapping
{
    using MetaService.Shared;

    public interface IRemoteMetaDataConfiguration
    {
        public IRemoteDataConverter Converter { get; }

        public RemoteMetaCallData[] RemoteMetaData { get; }

        string GetContractName(IRemoteCallContract contract);
        string GetRemoteMethodName(IRemoteCallContract contract);
        int CalculateMetaId(string contractName, IRemoteCallContract contract);
    }
}