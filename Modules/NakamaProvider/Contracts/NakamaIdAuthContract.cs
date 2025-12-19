using System;
using System.Collections.Generic;
using Nakama;
using UniGame.MetaBackend.Runtime;
using UniGame.MetaBackend.Runtime.Contracts;

[Serializable]
public class NakamaIdAuthContract : NakamaContract<string,NakamaAuthResult>,INakamaAuthContract
{
    public NakamaIdAuthData data = new();
    
    public override string Path => nameof(NakamaIdAuthContract);

    public INakamaAuthenticateData AuthData => data;
}

[Serializable]
public class NakamaIdAuthData : INakamaAuthenticateData
{
    public string id;
    public string userName;
    public bool create = true;
    public Dictionary<string, string> vars = null;
    public RetryConfiguration retryConfiguration = null;
    
    public string AuthTypeName => id;
}