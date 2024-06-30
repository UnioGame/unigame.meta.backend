namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using Cysharp.Threading.Tasks;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Meta Backend/Nakama Provider", fileName = "Nakama Provider")]
    public class NakamaProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public NakamaConnectionData connectionData;
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            // var remoteMetaDataConfig = await context
            //     .ReceiveFirstAsync<IRemoteMetaDataConfiguration>()
            //     .Timeout(TimeSpan.FromSeconds(connectionData.initTimeoutSec));
            
            var nakamaProvider = new NakamaMetaService(connectionData);
            return nakamaProvider;
        }
    }
}