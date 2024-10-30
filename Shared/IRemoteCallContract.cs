namespace UniGame.MetaBackend.Shared
{
    using System;

    public abstract class RemoteCallContract<TInput,TOutput> : IRemoteCallContract
    {
        public virtual string MethodName => string.Empty;
        public virtual Type OutputType => typeof(TOutput);
        public virtual Type InputType => typeof(TInput);
    }
    
    [Serializable]
    public class RemoteCallContract : IRemoteCallContract
    {
        public virtual Type InputType => typeof(string);
        public virtual Type OutputType => typeof(string);
        public virtual string MethodName => string.Empty;
    }
    
    public interface IRemoteCallContract
    {
        public string MethodName { get; }
        public Type OutputType { get; }
        public Type InputType { get; }
    }
    
}