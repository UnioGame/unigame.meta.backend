namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using Cysharp.Threading.Tasks;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Nakama Provider", fileName = "Nakama Provider")]
    public class NakamaProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public NakamaConnectionData connectionData;
        
        public override UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var nakamaProvider = new NakamaMetaService(connectionData);
            return UniTask.FromResult<IRemoteMetaProvider>(nakamaProvider);
        }
    }
}