namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using Game.Modules.unity.meta.service.Modules.WebProvider;
    using UniGame.MetaBackend.Runtime.WebService;

    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public class WebMetaProviderSettings
    {
        public const string SettingsKey = "settings";
        
#if ODIN_INSPECTOR
        [TabGroup(nameof(contracts))]
        [ListDrawerSettings(ListElementLabelName = "@name")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
#endif
        public List<WebApiEndPoint> contracts = new();
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif
        public bool enableApiGenerator = false;
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
        [ShowIf(nameof(enableApiGenerator))]
        [InlineProperty]
        [HideLabel]
#endif
        public WebApiSettings apiSettings = new();
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif
        [Header("debug")]
        public bool debugMode = false;
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif
        public bool enableLogs = true;
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif
        [Header("settings")]
        public bool useStreamingSettings = false;
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
        [ShowIf(nameof(useStreamingSettings))]
#endif
        public bool useStreamingUnderEditor = false;
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
        [ShowIf(nameof(useStreamingSettings))]
#endif
        public string streamingAssetsFileName = "web_meta_provider_settings.json";
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif

        public string defaultUrl = "http://localhost:5000";
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif

        public string defaultToken = "default_token";
        
        /// <summary>
        /// request retry count
        /// </summary>
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif
        public int requestRetry = 3;
        /// <summary>
        /// request operation timeout
        /// </summary>
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif
        public int timeout = 30;
        /// <summary>
        /// //single request timeout
        /// </summary>
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif

        public int requestTimeout = 30;
        
#if ODIN_INSPECTOR
        [TabGroup(SettingsKey)]
#endif
        public bool sendVersion = true;
        

    }
}