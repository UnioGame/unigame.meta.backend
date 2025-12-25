using System;
using System.Collections.Generic;
using Nakama;
using Newtonsoft.Json;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaPlayServicesAuthContract : NakamaContract<string,NakamaAuthResult>,INakamaAuthContract
{
    public NakamaGoogleAuthenticateData data = new();
    
    [JsonIgnore]
    public override string Path => nameof(NakamaIdAuthContract);

    [JsonIgnore]
    public INakamaAuthenticateData AuthData => data;
}



[Serializable]
public class NakamaGoogleAuthenticateData : INakamaAuthenticateData
{
    public string token;
    public string userName;
    public bool create = true;
    public bool linkAccount = false;
    
    public Dictionary<string, string> vars = null;
    public RetryConfiguration retryConfiguration = null;
        
    public string AuthTypeName => nameof(NakamaGoogleAuthenticateData);
}