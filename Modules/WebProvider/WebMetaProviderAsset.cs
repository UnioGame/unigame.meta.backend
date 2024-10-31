namespace Modules.WebServer
{
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Meta Backend/Web Backend Provider", fileName = "Web Backend Provider")]
    public class WebMetaProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public WebMetaProviderSettings settings = new();
        
        public override UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var service = new WebMetaProvider(settings);
            context.Publish<IWebMetaProvider>(service);
            return UniTask.FromResult<IRemoteMetaProvider>(service);
        }
    }
}