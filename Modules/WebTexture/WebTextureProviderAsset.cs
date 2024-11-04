namespace Modules.WebTexture
{
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Web Texture Provider", fileName = "Web Texture Provider")]
    public class WebTextureProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public WebTextureSettings settings = new();
        
        public override UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var service = new WebTextureProvider(settings, context.LifeTime);
            context.Publish<IWebTextureProvider>(service);
            return UniTask.FromResult<IRemoteMetaProvider>(service);
        }
    }
}