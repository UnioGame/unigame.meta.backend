namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Shared;
    using UniGame.Core.Runtime;
    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [CreateAssetMenu(menuName = "UniGame/MetaBackend/Mock Backend Provider", fileName = "Mock Backend Provider")]
    public class MockBackendProviderAsset : BackendMetaServiceAsset
    {
#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
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
        
#if ODIN_INSPECTOR
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
#endif
        public List<MockBackendData> mockBackendData = new();
    }
}