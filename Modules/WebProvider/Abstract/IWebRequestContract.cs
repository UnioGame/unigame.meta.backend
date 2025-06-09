namespace UniGame.MetaBackend.Runtime
{
    using UniGame.MetaBackend.Runtime.WebService;
    using UniGame.MetaBackend.Shared;

    public interface IWebRequestContract : IRemoteMetaContract,IFallbackContract
    {
        public WebRequestType RequestType { get; }
        public string Url { get; set;}
        public string Token { get; set;}
    }
}