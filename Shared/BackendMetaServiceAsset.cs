namespace UniGame.MetaBackend.Runtime
{
    using Core.Runtime;
    using Cysharp.Threading.Tasks;
    using Shared;
    using UnityEngine;

    public abstract class BackendMetaServiceAsset : ScriptableObject,IBackendMetaSource
    {
        public abstract UniTask<IRemoteMetaProvider> CreateAsync(IContext context);
    }
}