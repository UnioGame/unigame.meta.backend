namespace UniGame.MetaBackend.Runtime
{
    using UniGame.MetaBackend.Shared;

    public interface IWebTextureProvider : IRemoteMetaProvider
    {
        void SetToken(string token);
    }
}