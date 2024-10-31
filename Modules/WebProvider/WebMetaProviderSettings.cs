namespace Modules.WebServer
{
    using System;
    using System.Collections.Generic;
    using Game.Runtime.Services.WebService;
    using Sirenix.OdinInspector;

    [Serializable]
    public class WebMetaProviderSettings
    {
        public bool debugMode = false;
        public string defaultUrl = "http://localhost:5000";
        public string defaultToken = "default_token";
        public int timeout = 30;
        public bool sendVersion = true;
        
        [ListDrawerSettings(ListElementLabelName = "@name")]
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        public List<ApiEndPoint> contracts = new();
    }
}