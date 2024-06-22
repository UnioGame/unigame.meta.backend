namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using Cysharp.Threading.Tasks;
    using MetaService;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Game/Services/Meta Backend/Nakama Provider", fileName = "Nakama Provider")]
    public class NakamaProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public NakamaConnectionData connectionData;
        
        public override async UniTask<IBackendMetaService> CreateAsync(IContext context)
        {
            var nakamaProvider = new NakamaMetaService(connectionData);
            return nakamaProvider;
        }
    }
}