namespace MetaService.Shared
{
    using System;
    using UniGame.Core.Runtime;

    public interface IBackendMetaService : 
        IMetaConnection,
        IDisposable,
        ILifeTimeContext
    {
        
    }
}