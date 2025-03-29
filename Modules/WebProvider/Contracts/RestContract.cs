namespace Game.Modules.WebProvider.Contracts
{
    using global::Modules.WebServer;
    using Newtonsoft.Json;
    using Runtime.Services.WebService;
    using UniGame.MetaBackend.Shared;

    public abstract class RestContract<TInput,TOutput> : 
        SimpleMetaContract<TInput,TOutput>,
        IWebRequestContract
    {
        [JsonIgnore]
        public virtual WebRequestType RequestType => WebRequestType.None;
        [JsonIgnore]
        public virtual string Url { get; set; } = string.Empty;
        [JsonIgnore]
        public virtual string Token { get; set; } = string.Empty;
        
    }
}