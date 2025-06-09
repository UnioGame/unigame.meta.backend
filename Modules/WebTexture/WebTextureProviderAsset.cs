namespace UniGame.MetaBackend.Runtime
{
    using Cysharp.Threading.Tasks;

    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Web Texture Provider", fileName = "Web Texture Provider")]
    public class WebTextureProviderAsset : BackendMetaServiceAsset
    {
#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        public WebTextureSettings settings = new();
        
        public override UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var service = new WebTextureProvider(settings, context.LifeTime);
            context.Publish<IWebTextureProvider>(service);
            return UniTask.FromResult<IRemoteMetaProvider>(service);
        }
    }
}