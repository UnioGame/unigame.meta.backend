namespace MetaService.Runtime
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Game.Modules.ModelMapping;
    using UniGame.MetaBackend.Shared;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UniGame.Context.Runtime;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Represents a class that provides backend meta data for the game.
    /// </summary>
    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Backend Meta Source", 
        fileName = "Backend Meta Source")]
    public class BackendMetaSource : DataSourceAsset<IBackendMetaService>
    {
        [FormerlySerializedAs("backendMetaConfiguration")]
        [InlineEditor]
        [HideLabel]
        public RemoteMetaDataConfigAsset configuration;
        
        protected override async UniTask<IBackendMetaService> CreateInternalAsync(IContext context)
        {
            var asset = Instantiate(configuration);
            
            var backend = asset.settings;
            var remoteMeta = asset.configuration;

            context.Publish<IRemoteMetaDataConfiguration>(remoteMeta);
            
            var backendMetaType = backend.backendType;
            IRemoteMetaProvider defaultProvider = null;
            var providers = new Dictionary<int,IRemoteMetaProvider>();

            foreach (var backendType in backend.backendTypes)
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
            var service = new BackendMetaService(backendMetaType,providers,remoteMeta);
            
            return service;
        }

    }
}