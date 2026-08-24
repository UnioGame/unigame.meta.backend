using System;
using System.Collections.Generic;
using Nakama;
using Newtonsoft.Json;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaAppleAuthContract : NakamaContract<string, NakamaAuthResult>, INakamaAuthContract
{
    public NakamaAppleAuthenticateData data = new();

    [JsonIgnore]
    public override string Path => nameof(NakamaIdAuthContract);

    [JsonIgnore]
    public INakamaAuthenticateData AuthData => data;
}

[Serializable]
public class NakamaAppleAuthenticateData : INakamaAuthenticateData
{
    public string identityToken;
    public string userName;
    public bool create = true;
    public bool linkAccount;
    public Dictionary<string, string> vars;
    public RetryConfiguration retryConfiguration;

    public string AuthTypeName => nameof(NakamaAppleAuthenticateData);
}
