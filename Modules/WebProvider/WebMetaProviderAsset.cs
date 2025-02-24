namespace Modules.WebServer
{
    using System;
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
            {
                var settingsStreamingAsset = await LoadFromStreamingAssets();
                if (settingsStreamingAsset != null)
                {
                    webSettings.defaultUrl = settingsStreamingAsset.webUrl;
                }
            }
            
            GameLog.Log($"WebMetaProvider: {webSettings.defaultUrl}",Color.green);
            
            var service = new WebMetaProvider(webSettings);
            context.Publish<IWebMetaProvider>(service);
            return service;
        }
        
        
        [Button]
        public void SaveSettingsToStreamingAsset()
        {
            var webSettings = new WebMetaStreamingAsset()
            {
                webUrl = settings.defaultUrl,
            };
            
            StreamingAssetsUtils.SaveToStreamingAssets(settings.streamingAssetsFileName,webSettings);
        }
        
        [Button]
        public void LoadSettingsFromStreamingAsset()
        {
            LoadSettingsDataFromStreaming().Forget();
            async UniTask LoadSettingsDataFromStreaming()
            {
                var settingsValue = await LoadFromStreamingAssets();
                
                var validData = settingsValue != null;
                if(validData)
                    settings.defaultUrl = settingsValue.webUrl;
            }
        }
        
        public async UniTask<WebMetaStreamingAsset> LoadFromStreamingAssets()
        {
            var result = await StreamingAssetsUtils
                .LoadDataFromWeb<WebMetaStreamingAsset>(settings.streamingAssetsFileName);
            return result.success ? result.data : null;
        }
        
        
        [Serializable]
        public class WebMetaStreamingAsset
        {
            public string webUrl = string.Empty;
        }
        
    }
}