namespace UniGame.MetaBackend.Runtime
{
    using WebService;
    using Shared;

    public interface IWebRequestContract : IRemoteMetaContract,IFallbackContract
    {
        public WebRequestType RequestType { get; }
        public string Url { get; set;}
        public string Token { get; set;}
    }
}