namespace UniGame.MetaBackend.Runtime
{
    using Cysharp.Threading.Tasks;
    using Shared;
    using UniGame.Core.Runtime;
    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Web Backend Mock", fileName = "Web Backend Mock")]
    public class WebMetaProviderMockAsset : BackendMetaServiceAsset
    {
#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        public WebMetaProviderSettings settings = new();
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var service = new WebMetaMockProvider(settings);
            return service;
        }
    }
}