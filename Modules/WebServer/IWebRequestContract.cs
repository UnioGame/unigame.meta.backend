namespace Modules.WebServer
{
    using UniGame.MetaBackend.Shared;

    public interface IWebRequestContract : IRemoteMetaContract
    {
        public string Url { get; set;}
        public string Token { get; set;}
    }
}