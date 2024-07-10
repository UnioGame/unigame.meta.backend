namespace Game.Modules.ModelMapping
{
    using global::ModelMapping;
    using MetaService.Shared;

    public interface IRemoteMetaDataConfiguration
    {
        public IRemoteDataConverter Converter { get; }

        public RemoteMetaCallData[] RemoteMetaData { get; }
        public RemoteMetaNotificationData[] RemoteMetaNotificationData { get; }

        string GetContractName(IRemoteCallContract contract);
        string GetRemoteMethodName(IRemoteCallContract contract);
        int CalculateMetaId(IRemoteCallContract contract);
    }
}