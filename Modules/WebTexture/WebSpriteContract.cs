namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Newtonsoft.Json;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UnityEngine;

    [Serializable]
    public class WebSpriteContract : RemoteMetaContract<string, Sprite>
    {
        public string name = string.Empty;

        [JsonIgnore]
        public override object Payload => name;
        
        [JsonIgnore]
        public override string Path => name;
        
        [JsonIgnore]
        public Type FallbackType => typeof(object);
    }
}