namespace Game.Runtime.Services.Backend.Mock.Data
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Meta Backend/Mock Backend Provider", fileName = "Mock Backend Provider")]
    public class MockBackendProviderAsset : BackendMetaServiceAsset
    {

        public MockBackendDataConfig configuration = new();
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var mockBackendService = new MockBackendService(configuration);
            return mockBackendService;
        }
    }

    [Serializable]
    public class MockBackendDataConfig
    {
        public bool allowConnect = true;
        
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<MockBackendData> mockBackendData = new();

        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<MockBackendNotificationData> mockBackendNotificationData = new();
    }
}