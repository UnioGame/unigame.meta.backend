namespace UniGame.MetaBackend.Shared
{
    using System;
    using Newtonsoft.Json;

    public abstract class RemoteCallContract<TInput,TOutput> : IRemoteMetaContract<TInput,TOutput>
    {
        [JsonIgnore]
        public virtual object Payload => string.Empty;
        
        [JsonIgnore]
        public virtual string MethodName => string.Empty;
        
        [JsonIgnore]
        public virtual Type OutputType => typeof(TOutput);
        
        [JsonIgnore]
        public virtual Type InputType => typeof(TInput);
    }
    
    public interface IRemoteMetaContract<TIn,TOut> : IRemoteMetaContract
    {
    }
    
    [Serializable]
    public abstract class RemoteCallSelfContract<TOutput> : RemoteCallContract
    {
        [JsonIgnore]
        public override object Payload => this;
        
        [JsonIgnore]
        public override Type OutputType => typeof(TOutput);
        
        [JsonIgnore]
        public override Type InputType => GetType();
        
    }
    
    [Serializable]
    public class RemoteCallContract : IRemoteMetaContract
    {
        public virtual object Payload => string.Empty;
        public virtual Type InputType => typeof(string);
        public virtual Type OutputType => typeof(string);
        public virtual string MethodName => string.Empty;
    }
    
    public interface IRemoteMetaContract
    {
        public object Payload { get; }
        public string MethodName { get; }
        public Type OutputType { get; }
        public Type InputType { get; }
    }
    
}