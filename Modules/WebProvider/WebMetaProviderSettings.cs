namespace Modules.WebServer
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Game.Runtime.Services.WebService;
    using Game.Runtime.Tools;
    using Sirenix.OdinInspector;
    using UniModules;
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
        public string streamingAssetsFileName = "web_meta_provider_settings.json";
        
        public string defaultUrl = "http://localhost:5000";
        public string defaultToken = "default_token";
        public int timeout = 30;
        public bool sendVersion = true;
        
        [ListDrawerSettings(ListElementLabelName = "@name")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<WebApiEndPoint> contracts = new();

        
    }
}