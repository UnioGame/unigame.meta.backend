namespace MetaService.Shared
{
    using System;
    using Cysharp.Threading.Tasks;
    using MetaService.Shared.Data;

    public interface IMetaConnection : IDisposable
    {
        ConnectionState State { get; }
        
        UniTask<MetaConnectionResult> ConnectAsync(string deviceId);
        
        UniTask DisconnectAsync();
    }
}