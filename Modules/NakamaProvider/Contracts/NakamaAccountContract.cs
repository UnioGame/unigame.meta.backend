using System;
using Nakama;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaAccountContract : NakamaContract<string,IApiAccount>
{
    public override string Path => nameof(NakamaAccountContract);
}
