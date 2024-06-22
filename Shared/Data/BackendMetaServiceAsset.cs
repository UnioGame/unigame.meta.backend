namespace MetaService.Shared.Data
{
    using Cysharp.Threading.Tasks;
    using UniGame.Core.Runtime;
    using UnityEngine;

    public abstract class BackendMetaServiceAsset : ScriptableObject,IBackendMetaSource
    {
        public abstract UniTask<IBackendMetaService> CreateAsync(IContext context);
    }
}