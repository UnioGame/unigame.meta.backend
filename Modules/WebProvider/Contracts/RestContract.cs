namespace Game.Modules.WebProvider.Contracts
{
    using System;
    using global::UniGame.MetaBackend.Runtime;
    using Newtonsoft.Json;
    using UniGame.MetaBackend.Runtime.WebService;

    using UniGame.MetaBackend.Shared;
    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public abstract class RestContract<TInput,TOutput> : 
        RemoteMetaContract<TInput,TOutput>,
        IWebRequestContract
    {
#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        [SerializeField]
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
#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        [SerializeField]
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