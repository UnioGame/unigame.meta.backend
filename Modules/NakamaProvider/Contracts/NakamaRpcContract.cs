namespace UniGame.MetaBackend.Runtime.Contracts
{
    using System;
    using Newtonsoft.Json;
    using Shared;

    [Serializable]
    public class NakamaRpcContract : RemoteMetaContract<string, string> ,INakamaContract
    {
        [JsonIgnore]
        public string rpcName = string.Empty;

        public object payload = string.Empty;
        
        [JsonIgnore]
        public Type inputType;
        [JsonIgnore]
        public Type outputType;
        [JsonIgnore]
        public override string Path => rpcName;
        [JsonIgnore]
        
        public override object Payload => payload;
        
        [JsonIgnore]

        public override Type InputType => inputType;
        
        [JsonIgnore]
        public override Type OutputType => outputType;
    }
    
    [Serializable]
    public class NakamaRpcContract<TInput,TOutput> : RemoteMetaContract<TInput, TOutput> ,INakamaContract
    {
        [JsonIgnore]
        public string rpcName = string.Empty;
        [JsonIgnore]
        public TInput payload;
        
        [JsonIgnore]
        public override string Path => rpcName;
        [JsonIgnore]
        public override object Payload => payload;
        [JsonIgnore]
        public override Type InputType => typeof(TInput);
        [JsonIgnore]
        public override Type OutputType => typeof(TOutput);
    }
}