namespace Modules.WebServer
{
    using UniGame.MetaBackend.Shared;

    public interface IPostRequestContract : IRemoteMetaContract
    {
        public string Url { get; set; }
        string Token { get; set;}
    }
}