namespace UniGame.MetaBackend.Shared
{
    using System;
    using Newtonsoft.Json;

    [Serializable]
    public abstract class RemoteMetaContract : IRemoteMetaContract
    {
        public object payload;
        public Type output;
        public Type input;

        
        public object Payload => payload;
        public Type OutputType => output;
        public Type InputType => input;
        public virtual string Path => GetType().Name;
    }
    
    [Serializable]
    public abstract class RemoteMetaContract<TInput,TOutput> : IRemoteMetaContract<TInput,TOutput>
    {
        public TInput payload;
        
        [JsonIgnore]
        public object Payload => payload;
        
        [JsonIgnore]
        public Type OutputType => typeof(TOutput);
        
        [JsonIgnore]
        public Type InputType => typeof(TInput);
        
        [JsonIgnore]
        public virtual string Path => GetType().Name;
    }
    
    [Serializable]
    public abstract class RemoteMetaSelfContract<TOutput> : IRemoteMetaContract<IRemoteMetaContract,TOutput>
    {
        [JsonIgnore]
        public object Payload => this;
        
        [JsonIgnore]
        public Type OutputType => typeof(TOutput);
        
        [JsonIgnore]
        public Type InputType => GetType();
        
        [JsonIgnore]
        public virtual string Path => GetType().Name;
    }
    
}