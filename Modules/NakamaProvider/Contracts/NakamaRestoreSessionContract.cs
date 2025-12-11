using System;
using Nakama;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaRestoreSessionContract : NakamaContract<string,NakamaAuthResult>
{
    public override string Path => nameof(NakamaRestoreSessionContract);
}