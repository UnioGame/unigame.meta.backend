namespace UniGame.MetaBackend.Runtime
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Shared;

    public interface IWebMetaProvider : IRemoteMetaProvider
    {
        void SetToken(string token);
        Dictionary<string, string> SerializeToQuery(object payload);
        UniTask<RemoteMetaResult> ExecuteAsync(IRemoteMetaContract contract);
    }
}