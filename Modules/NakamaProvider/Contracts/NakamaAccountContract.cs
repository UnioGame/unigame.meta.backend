using System;
using Nakama;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaAccountContract : NakamaContract<string,IApiAccount>
{
    public override string Path => nameof(NakamaAccountContract);
}

[Serializable]
public class NakamaDeleteAccountContract : NakamaContract<string,bool>
{
    public override string Path => nameof(NakamaDeleteAccountContract);
}