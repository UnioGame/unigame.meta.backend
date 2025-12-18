using System;
using Nakama;
using Newtonsoft.Json;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaRestoreSessionContract : NakamaContract<string,NakamaAuthResult>
{
    [JsonIgnore]
    public override string Path => nameof(NakamaRestoreSessionContract);
}