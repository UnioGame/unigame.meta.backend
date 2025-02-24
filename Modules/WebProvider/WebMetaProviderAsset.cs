namespace Modules.WebServer
{
    using Cysharp.Threading.Tasks;
    using Game.Runtime.Tools;
    using Sirenix.OdinInspector;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UniGame.Core.Runtime;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Web Backend Provider", fileName = "Web Backend Provider")]
    public class WebMetaProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public WebMetaProviderSettings settings = new();
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var webSettings = settings;
            if (settings.useStreamingSettings)
                webSettings = await LoadFromStreamingAssets();
            
            GameLog.Log($"WebMetaProvider: {webSettings.defaultUrl}",Color.green);
            
            var service = new WebMetaProvider(webSettings);
            context.Publish<IWebMetaProvider>(service);
            return service;
        }
        
        
        [Button]
        public void SaveSettingsToSreaming()
        {
            StreamingAssetsUtils.SaveToStreamingAssets(settings.streamingAssetsFileName,settings);
        }
        
        [Button]
        public void LoadSettingsFromStreaming()
        {
            LoadSettingsDataFromStreaming().Forget();
            async UniTask LoadSettingsDataFromStreaming()
            {
                var settingsValue = await LoadFromStreamingAssets();
                settings = settingsValue;
            }
        }
        
        public async UniTask<WebMetaProviderSettings> LoadFromStreamingAssets()
        {
            var result = await StreamingAssetsUtils
                .LoadDataFromWeb<WebMetaProviderSettings>(settings.streamingAssetsFileName);
            return result is { success: true, data: not null } 
                ? result.data 
                : settings;
        }
        
        
    }
}