namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using Nakama;

    [Serializable]
    public class NakamaDeviceIdAuthenticateData : INakamaAuthenticateData
    {
        public string clientId;
        public string userName;
        public bool create = true;
        public Dictionary<string, string> vars = null;
        public RetryConfiguration retryConfiguration = null;
        
        public string AuthTypeName => nameof(NakamaDeviceIdAuthenticateData);
    }
}