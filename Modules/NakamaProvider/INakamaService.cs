namespace UniGame.MetaBackend.Runtime
{
    using Cysharp.Threading.Tasks;
    using GameFlow.Runtime;
    using Shared;

    public interface INakamaService : IRemoteMetaProvider, IGameService
    {
        UniTask<NakamaConnectionResult> SignInAsync(INakamaAuthenticateData authenticateData);

        UniTask<bool> SignOutAsync();
    }
}