namespace UniGame.MetaBackend.Shared
{
    using System;
    using Newtonsoft.Json;

    [Serializable]
    public abstract class RemoteMetaContract<TInput, TOutput,TError> : RemoteMetaContract<TInput,TOutput>, IFallbackContract
    {
        [JsonIgnore]
        public virtual Type FallbackType => typeof(TError);
    }

    [Serializable]
    public abstract class RemoteMetaContract<TInput,TOutput> : IRemoteMetaContract
    {   
        [JsonIgnore]
        public virtual object Payload => string.Empty;

        [JsonIgnore]
        public virtual string Path => string.Empty;
        
        [JsonIgnore]
        public virtual Type OutputType => typeof(TOutput);
        
        [JsonIgnore]
        public virtual Type InputType => typeof(TInput);
    }
    
    /// <summary>
    /// Self contract use its own object as input type and payload
    /// </summary>
    [Serializable]
    public abstract class RemoteSelfMetaContract<TInput,TOutput> : RemoteMetaContract<TInput,TOutput>
    {   
        [JsonIgnore]
        public override object Payload => this;

        /// <summary>
        /// return input as contract type
        /// </summary>
        [JsonIgnore]
        public sealed override Type InputType => GetType();
    }
    
    [Serializable]
    public abstract class RemoteMetaContract<TOutput> : RemoteMetaContract
    {
        [JsonIgnore]
        public override object Payload => this;
        
        [JsonIgnore]
        public override Type OutputType => typeof(TOutput);
        
        [JsonIgnore]
        public override Type InputType => GetType();
        
    }
    
    [Serializable]
    public class RemoteMetaContract : IRemoteMetaContract
    {
        public virtual object Payload => string.Empty;
        public virtual Type InputType => typeof(string);
        public virtual Type OutputType => typeof(string);
        public virtual string Path => string.Empty;
    }
    
    public interface IRemoteMetaContract
    {
        public object Payload { get; }
        public string Path { get; }
        public Type OutputType { get; }
        public Type InputType { get; }
    }
    
    public interface IFallbackContract
    {
        public Type FallbackType { get; }
    }
    
}