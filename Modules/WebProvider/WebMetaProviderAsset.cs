namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using UniGame.MetaBackend.Runtime.WebService;
    using Game.Runtime.Tools;
    using Sirenix.OdinInspector;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UniGame.Core.Runtime;
    using UniGame.Runtime.Utils;
    using UnityEngine;
    using Game.Modules.Meta.Runtime;
    
#if UNITY_EDITOR
    using Game.Modules.ModelMapping;
    using UnityEditor;
    using UniModules.Editor;
#endif
    
    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Web Backend Provider", fileName = "Web Backend Provider")]
    public class WebMetaProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public WebMetaProviderSettings settings = new();
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var webSettings = settings;
            var useStreaming = settings.useStreamingSettings && 
                               (settings.useStreamingUnderEditor || !Application.isEditor);
            if (useStreaming)
            {
                var settingsStreamingAsset = await LoadFromStreamingAssets();
                if (settingsStreamingAsset != null)
                {
                    webSettings.defaultUrl = settingsStreamingAsset.webUrl;
                    webSettings.requestTimeout = settingsStreamingAsset.requestTimeout;
                }
            }
            
            GameLog.Log($"WebMetaProvider: {webSettings.defaultUrl}",Color.green);
            
            var service = new WebMetaProvider(webSettings);
            context.Publish<IWebMetaProvider>(service);
            return service;
        }
        
        [Button]
        [PropertyOrder(-1)]
        public void LoadContracts()
        {
#if UNITY_EDITOR
            var contracts = settings.contracts;
            var webContractTypes = TypeCache.GetTypesDerivedFrom<IWebRequestContract>();
            foreach (var contractType in webContractTypes)
            {
                if (contractType.IsAbstract||contractType.IsInterface)continue;
                var targetContact =
                    contracts.FirstOrDefault(x => x.contract == contractType);
                if(targetContact !=null) continue;

                var path = string.Empty;
                var url = string.Empty;
                
                IWebRequestContract contractItem = null;
                
                if (contractType.HasDefaultConstructor())
                {
                    if (contractType.CreateWithDefaultConstructor() is IWebRequestContract contractInstance)
                    {
                        contractItem = contractInstance;
                        path = contractInstance.Path;
                        url = contractInstance.Url;
                    }
                }
                
                var contractName = BackendMetaTools.GetContractName(contractType);
                var webMethod = WebRequestType.Get;
                if (contractName.StartsWith(WebRequestType.Get.ToStringFromCache(),StringComparison.OrdinalIgnoreCase))
                {
                    webMethod = WebRequestType.Get;
                }
                if (contractName.StartsWith(WebRequestType.Post.ToStringFromCache(),StringComparison.OrdinalIgnoreCase))
                {
                    webMethod = WebRequestType.Post;
                }
                if (contractName.StartsWith(WebRequestType.Patch.ToStringFromCache(),StringComparison.OrdinalIgnoreCase))
                {
                    webMethod = WebRequestType.Patch;
                }

                if (contractItem != null && contractItem.RequestType != WebRequestType.None)
                    webMethod = contractItem.RequestType;
                
                contracts.Add(new WebApiEndPoint()
                {
                    contract = contractType,
                    name = contractName,
                    requestType = webMethod,
                    path = path,
                    url = url,
                });
                
            }

            var serviceConfigs = AssetEditorTools
                .GetAssets<RemoteMetaDataConfigAsset>();

            foreach (var serviceConfig in serviceConfigs)
            {
                serviceConfig.UpdateRemoteMetaData();
            }
            
            this.MarkDirty();
#endif
        }
        
        [Button]
        [PropertyOrder(-1)]
        public void SaveSettingsToStreamingAsset()
        {
            var webSettings = new WebMetaStreamingAsset()
            {
                webUrl = settings.defaultUrl,
            };
            
            StreamingAssetsUtils.SaveToStreamingAssets(settings.streamingAssetsFileName,webSettings);
        }
        
        [Button]
        [PropertyOrder(-1)]
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
            public int requestTimeout = 30;
        }
        
    }
}