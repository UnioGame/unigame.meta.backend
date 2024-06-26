namespace Game.Modules.ModelMapping
{
    using System;
    using Newtonsoft.Json;

    [Serializable]
    public class JsonRemoteDataConverter : IRemoteDataConverter
    {
        public TModel Convert<TModel>(string id, string method, string data)
        {
            return JsonConvert.DeserializeObject<TModel>(data);
        }
    }
}