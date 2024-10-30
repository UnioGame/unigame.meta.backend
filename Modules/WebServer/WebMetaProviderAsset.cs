namespace Modules.WebServer
{
    using Cysharp.Threading.Tasks;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Meta Backend/Web Backend Provider", fileName = "Web Backend Provider")]
    public class WebMetaProviderAsset : BackendMetaServiceAsset
    {
        public override UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var service = new WebMetaProvider();
            return UniTask.FromResult<IRemoteMetaProvider>(service);
        }
    }
}