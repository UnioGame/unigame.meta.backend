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
    public class BackendMetaSource : DataSourceAsset
    {
#if ODIN_INSPECTOR
        [InlineEditor]
        [HideLabel]
#endif
        [FormerlySerializedAs("backendMetaConfiguration")]
        public ContractsConfigurationAsset configuration;
        

        protected override async UniTask<IContext> OnRegisterAsync(IContext context)
        {
            var asset = Instantiate(configuration);

            var settings = asset.settings;
            var remoteMeta = asset.configuration;
            context.Publish<IRemoteMetaDataConfiguration>(remoteMeta);
            
            var service = new BackendMetaService(settings,context, remoteMeta);
            context.Publish<IBackendMetaService>(service);

            return context;
        }
    }
}