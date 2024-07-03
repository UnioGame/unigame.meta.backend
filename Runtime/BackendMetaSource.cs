namespace MetaService.Runtime
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Game.Modules.ModelMapping;
    using Shared;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UniGame.GameFlow.Runtime.Services;
    using UnityEngine;

    /// <summary>
    /// Represents a class that provides backend meta data for the game.
    /// </summary>
    [CreateAssetMenu(menuName = "UniGame/Meta Backend/Backend Meta Source", fileName = "Backend Meta Source")]
    public class BackendMetaSource : DataSourceAsset<IBackendMetaService>
    {
        [InlineProperty]
        [HideLabel]
        public BackendMetaConfiguration backendMetaConfiguration = new();

        protected override async UniTask<IBackendMetaService> CreateInternalAsync(IContext context)
        {
            var meta = backendMetaConfiguration.meta;
            var backend = meta.settings;
            var data = backendMetaConfiguration.backend;
            var remoteMetaAsset = Instantiate(backendMetaConfiguration.meta);
            var remoteMeta = remoteMetaAsset.configuration;

            context.Publish<IRemoteMetaDataConfiguration>(remoteMeta);
            
            var backendMetaType = backend.backendType;
            IRemoteMetaProvider defaultProvider = null;
            var providers = new Dictionary<int,IRemoteMetaProvider>();

            foreach (var backendType in data.Types)
            {
                var providerSource = Instantiate(backendType.Provider);
                var metaProvider = await providerSource.CreateAsync(context);
                providers[backendType.Id] = metaProvider;
                if (backendType.Id == backendMetaType)
                    defaultProvider = metaProvider;
                
                break;
            }

            if (defaultProvider == null)
            {
                 Debug.LogError($"Backend provider for type {backendMetaType} not found.");
                 return null;
            }

            context.Publish<IRemoteMetaProvider>(defaultProvider);
            var service = new BackendMetaService(defaultProvider,providers,remoteMeta);
            return service;
        }

    }
}