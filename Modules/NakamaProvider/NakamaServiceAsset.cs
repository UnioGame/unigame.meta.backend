namespace UniGame.MetaBackend.Runtime
{
    using Core.Runtime;
    using Cysharp.Threading.Tasks;
    using Shared;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/MetaBackend/Nakama Provider", fileName = "Nakama Provider")]
    public class NakamaServiceAsset : BackendMetaServiceAsset
    {
        public NakamaSettings nakamaSettings;
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var settings = Instantiate(this).nakamaSettings;
            var nakamaConnection = new NakamaConnection();
            var service = new NakamaService(settings,nakamaConnection);

            context.Publish(settings);
            context.Publish<INakamaConnection>(nakamaConnection);
            context.Publish<INakamaService>(service);

            return service;
        }
    }
}