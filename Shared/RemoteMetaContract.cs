namespace UniGame.MetaBackend.Shared
{
    using System;
    using Newtonsoft.Json;

    [Serializable]
    public class RemoteMetaContract : IRemoteMetaContract
    {
        public object payload;
        public Type output;
        public Type input;
        
        public object Payload => payload;
        public Type Output => output;
        public Type Input => input;
    }
    
    [Serializable]
    public abstract class RemoteMetaContract<TInput,TOutput> : IRemoteMetaContract<TInput,TOutput>
    {
        public TInput payload;
        
        public object Payload => payload;
        public Type Output => typeof(TOutput);
        public Type Input => typeof(TInput);
    }
    
    [Serializable]
    public abstract class RemoteMetaSelfContract<TInput,TOutput> : IRemoteMetaContract<TInput,TOutput>
    {
        [JsonIgnore]
        public object Payload => this;
        
        [JsonIgnore]
        public Type Output => typeof(TOutput);
        
        [JsonIgnore]
        public Type Input => GetType();
    }
    
}