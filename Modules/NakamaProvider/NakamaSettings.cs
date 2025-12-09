namespace UniGame.MetaBackend.Runtime
{
    using System;
    using UnityEngine;

    [Serializable]
    public class NakamaSettings
    {
        [Header("Connection Settings")]
        public int maxRetries = 5;
        public int retryDelayMs = 1000;
        public int timeoutSec = 5;
        public string healthCheckPath = "/healthcheck";
        public int refreshTokenInterval = 60 * 60 * 6;
        public int autoRefreshIntervalSec = 60;
        public bool autoRefreshSession = true;
        public bool useSocketMainThread = true;
        
        [Header("Debug Settings")]
        public bool enableLogging = false;
        
        [Header("Server Endpoints")]
        public NakamaEndpoint[] servers = new[] { new NakamaEndpoint() };
    }
    
    [Serializable]
    public class NakamaEndpoint
    {
        public string scheme = "http";
        public string host =  "localhost";
        public int port =  7350;
        public int gRPCPort =  7349;
        public string serverKey = "defaultkey";
    }
}