namespace Game.Modules.ModelMapping
{
    using System;
    using Newtonsoft.Json;

    [Serializable]
    public class JsonRemoteDataConverter : IRemoteDataConverter
    {
        public Object Convert(Type type,string data)
        {
            return JsonConvert.DeserializeObject(data,type);
        }
    }
}