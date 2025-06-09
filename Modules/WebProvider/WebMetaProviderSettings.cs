namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using Game.Modules.unity.meta.service.Modules.WebProvider;
    using UniGame.MetaBackend.Runtime.WebService;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [Serializable]
    public class WebMetaProviderSettings
    {
        public const string SettingsKey = "settings";
        
        [TabGroup(nameof(contracts))]
        [ListDrawerSettings(ListElementLabelName = "@name")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<WebApiEndPoint> contracts = new();
        
        [TabGroup(SettingsKey)]
        public bool enableApiGenerator = false;
        
        [TabGroup(SettingsKey)]
        [ShowIf(nameof(enableApiGenerator))]
        [InlineProperty]
        [HideLabel]
        public WebApiSettings apiSettings = new();
        
        [TabGroup(SettingsKey)]
        [Header("debug")]
        public bool debugMode = false;
        
        [TabGroup(SettingsKey)]
        public bool enableLogs = true;
        
        [TabGroup(SettingsKey)]
        [Header("settings")]
        public bool useStreamingSettings = false;
        
        [TabGroup(SettingsKey)]
        [ShowIf(nameof(useStreamingSettings))]
        public bool useStreamingUnderEditor = false;
        
        [TabGroup(SettingsKey)]
        [ShowIf(nameof(useStreamingSettings))]
        public string streamingAssetsFileName = "web_meta_provider_settings.json";
        
        [TabGroup(SettingsKey)]
        public string defaultUrl = "http://localhost:5000";
        [TabGroup(SettingsKey)]
        public string defaultToken = "default_token";
        
        /// <summary>
        /// request retry count
        /// </summary>
        [TabGroup(SettingsKey)]
        public int requestRetry = 3;
        /// <summary>
        /// request operation timeout
        /// </summary>
        [TabGroup(SettingsKey)]
        public int timeout = 30;
        /// <summary>
        /// //single request timeout
        /// </summary>
        [TabGroup(SettingsKey)]
        public int requestTimeout = 30;
        
        [TabGroup(SettingsKey)]
        public bool sendVersion = true;
        

    }
}