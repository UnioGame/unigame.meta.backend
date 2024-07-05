namespace MetaService.Shared
{
    using Cysharp.Threading.Tasks;

    public interface IRemoteMetaMatchmaking
    {
        UniTask<string> AddMatchmakerAsync();
    }
}