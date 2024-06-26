namespace MetaService.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using Data;
    using UniGame.Core.Runtime;

    public interface IBackendMetaService : 
        IMetaConnection,
        IDisposable,
        ILifeTimeContext
    {
        
        IObservable<MetaDataResult> DataStream { get; }
        
        UniTask<MetaDataResult> GetDataAsync(Type type);
        
        UniTask PostDataAsync(Type method, object data = null);
        
    }
}