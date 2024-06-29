namespace MetaService.Shared
{
    using System;

    public abstract class RemoteCallContract<TInput,TOutput> : IRemoteCallContract
    {
        public virtual string MethodName => string.Empty;
        public virtual Type OutputType => typeof(TOutput);
        public virtual Type InputType => typeof(TInput);
    }
    
    public interface IRemoteCallContract
    {
        public string MethodName { get; }
        public Type OutputType { get; }
        public Type InputType { get; }
    }
    
}