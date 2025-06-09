namespace UniGame.MetaBackend.Runtime
{
    using Cysharp.Threading.Tasks;
    using Shared;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Web Backend Mock", fileName = "Web Backend Mock")]
    public class WebMetaProviderMockAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public WebMetaProviderSettings settings = new();
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var service = new WebMetaMockProvider(settings);
            return service;
        }
    }
}