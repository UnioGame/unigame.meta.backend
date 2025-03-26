namespace Game.Modules.WebProvider.Contracts
{
    using global::Modules.WebServer;
    using Newtonsoft.Json;
    using UniGame.MetaBackend.Shared;

    public abstract class RestContract<TInput,TOutput> : 
        SimpleMetaContract<TInput,TOutput>,
        IWebRequestContract
    {
        
        [JsonIgnore]
        public virtual string Url { get; set; } = string.Empty;
        [JsonIgnore]
        public virtual string Token { get; set; } = string.Empty;
        
    }
}