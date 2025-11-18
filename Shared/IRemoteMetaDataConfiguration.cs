namespace Game.Modules.ModelMapping
{
    using UniGame.MetaBackend.Shared;

    public interface IRemoteMetaDataConfiguration
    {
        public int HistorySize { get; }
        
        public IRemoteDataConverter Converter { get; }

        public RemoteMetaData[] RemoteMetaData { get; }

        string GetContractName(IRemoteMetaContract contract);
        string GetRemoteMethodName(IRemoteMetaContract contract);
        int CalculateMetaId(IRemoteMetaContract contract);
    }
}