namespace MetaService.Runtime
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Game.Modules.ModelMapping;
    using UniGame.MetaBackend.Shared;

    using UniGame.Core.Runtime;
    using UniGame.Context.Runtime;
    using UnityEngine;
    using UnityEngine.Serialization;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    /// <summary>
    /// Represents a class that provides backend meta data for the game.
    /// </summary>
    [CreateAssetMenu(menuName = "UniGame/MetaBackend/Backend Meta Source", fileName = "Backend Meta Source")]
    public class BackendMetaSource : DataSourceAsset<IBackendMetaService>
    {
#if ODIN_INSPECTOR
        [InlineEditor]
        [HideLabel]
#endif
        [FormerlySerializedAs("backendMetaConfiguration")]
        public ContractsConfigurationAsset configuration;
        
        protected override async UniTask<IBackendMetaService> CreateInternalAsync(IContext context)
        {
            var asset = Instantiate(configuration);
            
            var settings = asset.settings;
            var remoteMeta = asset.configuration;

            context.Publish<IRemoteMetaDataConfiguration>(remoteMeta);
            
            var backendMetaType = settings.backendType;
            IRemoteMetaProvider defaultProvider = null;
            var providers = new Dictionary<int,IRemoteMetaProvider>();

            foreach (var backendType in settings.backendTypes)
            {
                var providerSource = Instantiate(backendType.Provider);
                var metaProvider = await providerSource.CreateAsync(context);
                metaProvider.AddTo(context.LifeTime);
                
                providers[backendType.Id] = metaProvider;
                if (backendType.Id == backendMetaType)
                    defaultProvider = metaProvider;
            }

            if (defaultProvider == null)
            {
                 Debug.LogError($"Backend provider for type {backendMetaType} not found.");
                 return null;
            }

            context.Publish<IRemoteMetaProvider>(defaultProvider);
            var service = new BackendMetaService(settings.useDefaultBackendFirst,
                settings.historySize,
                backendMetaType,providers,remoteMeta);
            
            return service;
        }

    }
}