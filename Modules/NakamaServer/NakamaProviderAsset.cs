namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using System;
    using Cysharp.Threading.Tasks;
    using MetaService;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using Modules.ModelMapping;
    using Sirenix.OdinInspector;
    using UniGame.Context.Runtime.Extension;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Game/Services/Meta Backend/Nakama Provider", fileName = "Nakama Provider")]
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