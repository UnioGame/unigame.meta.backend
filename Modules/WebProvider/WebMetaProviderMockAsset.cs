namespace Modules.WebServer
{
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Web Backend Mock", fileName = "Web Backend Mock")]
    public class WebMetaProviderMockAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public WebMetaProviderSettings settings = new();
        
        public override UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var service = new WebMetaMockProvider(settings);
            return UniTask.FromResult<IRemoteMetaProvider>(service);
        }
    }
}