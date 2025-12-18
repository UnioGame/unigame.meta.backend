using System;
using Nakama;
using UniGame.MetaBackend.Runtime;
using UniGame.MetaBackend.Runtime.Contracts;

[Serializable]
public class NakamaIdAuthContract : NakamaContract<string,NakamaAuthResult>,INakamaAuthContract
{
    public NakamaDeviceIdAuthenticateData data = new();
    
    public override string Path => nameof(NakamaIdAuthContract);

    public INakamaAuthenticateData AuthData => data;
}