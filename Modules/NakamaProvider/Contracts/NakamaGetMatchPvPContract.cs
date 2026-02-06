using System;
using Newtonsoft.Json;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaGetMatchPvPContract : NakamaContract<NakamaGetMatchPvPContract,NakamaGetMatchPvPResult>
{    
  [JsonIgnore]
  public override string Path => nameof(NakamaGetMatchPvPContract);
}

public class NakamaGetMatchPvPResult
{
  public bool success;
}