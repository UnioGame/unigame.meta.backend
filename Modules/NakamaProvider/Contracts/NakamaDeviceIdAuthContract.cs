namespace UniGame.MetaBackend.Runtime.Contracts
{
    using System;
    using System.Collections.Generic;
    using Nakama;
    using Newtonsoft.Json;

    [Serializable]
    public class NakamaAuthContract : NakamaContract<INakamaAuthenticateData,NakamaServiceResult>,INakamaAuthContract
    {
        public INakamaAuthenticateData authData;

        [JsonIgnore]
        public override string Path => "nakama_auth";

        [JsonIgnore]
        public override object Payload => authData;

        [JsonIgnore]
        public string AuthTypeName => authData.AuthTypeName;

        [JsonIgnore]
        public INakamaAuthenticateData AuthData => authData;
    }
    
    
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