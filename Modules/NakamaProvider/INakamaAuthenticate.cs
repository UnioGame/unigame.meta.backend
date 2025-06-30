namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Cysharp.Threading.Tasks;
    using Nakama;

    public interface INakamaAuthenticate : IDisposable
    {
        UniTask<NakamaSessionResult> AuthenticateAsync(IClient client,INakamaAuthenticateData authenticateData);
    }
}