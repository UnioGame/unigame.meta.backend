using System;
using Newtonsoft.Json;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaLogoutContract : NakamaContract<NakamaLogoutContract,NakamaLogoutResult>
{
    
    [JsonIgnore]
    public override string Path => nameof(NakamaLogoutContract);

}

[Serializable]
public class NakamaLogoutResult
{
    public static readonly NakamaLogoutResult Success = new NakamaLogoutResult(){ success =  true };
    public static readonly NakamaLogoutResult Failed = new NakamaLogoutResult(){ success =  false };
    
    public bool success;
}