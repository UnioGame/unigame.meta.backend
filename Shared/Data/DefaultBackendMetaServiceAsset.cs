namespace MetaService.Shared.Data
{
    using Cysharp.Threading.Tasks;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Game/Services/Meta Backend/Default Provider", fileName = "Default Provider")]
    public class DefaultBackendMetaServiceAsset : BackendMetaServiceAsset,IBackendMetaSource
    {
        [SerializeReference]
        public IBackendMetaService service;

        public override async UniTask<IBackendMetaService> CreateAsync(IContext context)
        {
            return service;
        }
    }
}