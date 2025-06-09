namespace Game.Modules.WebProvider.Contracts
{
    using System;
    using global::UniGame.MetaBackend.Runtime;
    using Newtonsoft.Json;
    using UniGame.MetaBackend.Runtime.WebService;
    using Sirenix.OdinInspector;
    using UniGame.MetaBackend.Shared;
    using UnityEngine;

    [Serializable]
    public abstract class RestContract<TInput,TOutput> : 
        RemoteMetaContract<TInput,TOutput>,
        IWebRequestContract
    {
        [SerializeField]
        [InlineProperty]
        [HideLabel]
        public TInput input;

        [JsonIgnore]
        public override object Payload => input;
        [JsonIgnore]
        public virtual WebRequestType RequestType => WebRequestType.None;
        [JsonIgnore]
        public virtual string Url { get; set; } = string.Empty;
        [JsonIgnore]
        public virtual string Token { get; set; } = string.Empty;
        [JsonIgnore]
        public virtual Type FallbackType { get; set; } = typeof(string);
    }

    [Serializable]
    public abstract class RestContract<TInput, TOutput,TError> : RemoteMetaContract<TInput,TOutput,TError>,
        IWebRequestContract
    {
        [SerializeField]
        [InlineProperty]
        [HideLabel]
        public TInput input;

        [JsonIgnore]
        public override object Payload => input;
        [JsonIgnore]
        public virtual WebRequestType RequestType => WebRequestType.None;
        [JsonIgnore]
        public virtual string Url { get; set; } = string.Empty;
        [JsonIgnore]
        public virtual string Token { get; set; } = string.Empty;
        [JsonIgnore]
        public override Type FallbackType => typeof(TError);
    }
}