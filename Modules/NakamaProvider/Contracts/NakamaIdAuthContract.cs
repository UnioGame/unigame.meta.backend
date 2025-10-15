using System;
using Nakama;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaIdAuthContract : NakamaContract<string,IApiAccount>,INakamaAuthContract
{
    public NakamaDeviceIdAuthenticateData data = new();
    
    public override string Path => nameof(NakamaIdAuthContract);

    public INakamaAuthenticateData AuthData => data;
}