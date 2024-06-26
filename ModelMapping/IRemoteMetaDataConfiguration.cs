namespace Game.Modules.ModelMapping
{
    public interface IRemoteMetaDataConfiguration
    {
        public IRemoteDataConverter Converter { get; }
        
        public string GetMethodTemplate { get; }
        
        public string PostMethodTemplate { get; }
        
        public MetaRemoteItem[] RemoteMetaData { get; }
    }
}