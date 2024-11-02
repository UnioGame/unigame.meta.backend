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
        public string defaultUrl = "http://localhost:5000";
        public string defaultToken = "default_token";
        public int timeout = 30;
        public bool sendVersion = true;
        
        [ListDrawerSettings(ListElementLabelName = "@name")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<WebApiEndPoint> contracts = new();
    }
}