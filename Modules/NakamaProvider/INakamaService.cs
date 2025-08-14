namespace UniGame.MetaBackend.Runtime
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using GameFlow.Runtime;
    using Shared;

    public interface INakamaService : IRemoteMetaProvider, IGameService
    {
        UniTask<NakamaConnectionResult> SignInAsync(
            INakamaAuthenticateData authenticateData,
            CancellationToken cancellationToken = default);

        UniTask<bool> SignOutAsync();
    }
}