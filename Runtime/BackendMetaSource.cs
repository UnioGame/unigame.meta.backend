namespace MetaService.Runtime
{
    using Cysharp.Threading.Tasks;
    using Game.Modules.ModelMapping;
    using Runtime;
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
            
            var settings = backendMetaConfiguration.settings;
            var data = backendMetaConfiguration.backend;
            var remoteMetaAsset = Instantiate(backendMetaConfiguration.metaDataAsset);
            var remoteMeta = remoteMetaAsset.configuration;

            context.Publish<IRemoteMetaDataConfiguration>(remoteMeta);
            
            var backendMetaType = settings.backendType;
            IRemoteMetaProvider remoteMetaProvider = null;

            foreach (var backendType in data.Types)
            {
                if (backendType.Id != backendMetaType) continue;
                var provider = Instantiate(backendType.Provider);
                remoteMetaProvider = await provider.CreateAsync(context);
                break;
            }

            if (remoteMetaProvider == null)
            {
                 Debug.LogError($"Backend provider for type {backendMetaType} not found.");
                 return null;
            }

            context.Publish<IRemoteMetaProvider>(remoteMetaProvider);
            
            var service = new BackendMetaService(remoteMeta,remoteMetaProvider);
            return service;
        }

    }
}