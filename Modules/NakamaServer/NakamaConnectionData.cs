namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using System;

    [Serializable]
    public class NakamaConnectionData
    {
        public string scheme = "http";
        public string host;
        public int port;
        public string serverKey = "defaultkey";
        public int tokenExpireSec = 60 * 60 * 24;
        public string langTag = "en";
        
        public int retryCount = 3;
        public int retryDelayMs = 200;
        public int requestTimeoutSec = 10;
        
        //session settings
        public bool autoRefreshSession = true;
        
        //socket settings
        public bool useSocketMainThread = true;
        public int socketConnectTimeoutSec = 60;
        public bool appearOnline = true;
        
        //debug settings
        public float initTimeoutSec = 10;
    }
}