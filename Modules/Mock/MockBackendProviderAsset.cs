namespace Game.Runtime.Services.Backend.Mock.Data
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Mock Backend Provider", fileName = "Mock Backend Provider")]
    public class MockBackendProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
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
    }
}