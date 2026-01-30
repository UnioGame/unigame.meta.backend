using System;
using Nakama;
using Newtonsoft.Json;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaUpdateAccountContract : NakamaContract<NakamaAccountData,IApiAccount>
{
    public NakamaAccountData data = new();

    [JsonIgnore]
    public override object Payload => data;

    [JsonIgnore]
    public override string Path => nameof(NakamaIdAuthContract);
}

[Serializable]
public class NakamaAccountData
{
    public string displayName = string.Empty;
    public string avatarUrl = string.Empty;
}
