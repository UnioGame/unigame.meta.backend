namespace Modules.WebServer
{
    using UniGame.MetaBackend.Shared;

    public interface IGetRequestContract : IRemoteMetaContract
    {
        public string Url { get; set;}
        public string Token { get; set;}
    }
}