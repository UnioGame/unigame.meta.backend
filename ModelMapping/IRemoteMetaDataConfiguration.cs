namespace Game.Modules.ModelMapping
{
    public interface IRemoteMetaDataConfiguration
    {
        public IRemoteDataConverter Converter { get; }

        public RemoteMetaData[] RemoteMetaData { get; }
    }
}