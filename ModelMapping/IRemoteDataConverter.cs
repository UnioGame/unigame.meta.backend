namespace Game.Modules.ModelMapping
{
    public interface IRemoteDataConverter
    {
        public TModel Convert<TModel>(string id, string method,string data);
    }
}