namespace Game.Runtime.Services.Backend.Mock.Data
{
    using Cysharp.Threading.Tasks;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Game/Services/Meta Backend/Mock Backend Provider", fileName = "Mock Backend Provider")]
    public class MockBackendServiceSource : BackendMetaServiceAsset
    {
        public override async UniTask<IBackendMetaService> CreateAsync(IContext context)
        {
            var mockBackendService = new MockBackendService();
            return mockBackendService;
        }
    }
}