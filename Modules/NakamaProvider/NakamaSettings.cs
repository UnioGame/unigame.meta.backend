﻿namespace UniGame.MetaBackend.Runtime
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
        public string healthCheckPath = "/health";
        public int refreshTokenInterval = 60 * 60 * 6;
        public bool autoRefreshSession = true;
        public bool useSocketMainThread = true;
        
        [Header("Server Endpoints")]
        public NakamaEndpoint[] servers = new[] { new NakamaEndpoint() };
    }
    
    [Serializable]
    public class NakamaEndpoint
    {
        public string scheme = "http";
        public string host =  "localhost";
        public int port =  7350;
        public string serverKey = "defaultkey";
    }
}