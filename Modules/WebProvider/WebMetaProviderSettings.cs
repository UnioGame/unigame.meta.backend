namespace Modules.WebServer
{
    using System;
    using System.Collections.Generic;
    using Game.Runtime.Services.WebService;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [Serializable]
    public class WebMetaProviderSettings
    {
        [Header("debug")]
        public bool debugMode = false;
        public bool enableLogs = true;
        
        [Header("settings")]
        public bool useStreamingSettings = false;
        
        [ShowIf(nameof(useStreamingSettings))]
        public bool useStreamingUnderEditor = false;
        
        [ShowIf(nameof(useStreamingSettings))]
        public string streamingAssetsFileName = "web_meta_provider_settings.json";
        
        public string defaultUrl = "http://localhost:5000";
        public string defaultToken = "default_token";
        
        /// <summary>
        /// request retry count
        /// </summary>
        public int requestRetry = 3;
        /// <summary>
        /// request operation timeout
        /// </summary>
        public int timeout = 30;
        /// <summary>
        /// //single request timeout
        /// </summary>
        public int requestTimeout = 30;
        
        public bool sendVersion = true;
        
        [ListDrawerSettings(ListElementLabelName = "@name")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<WebApiEndPoint> contracts = new();
    }
}