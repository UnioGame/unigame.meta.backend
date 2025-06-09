namespace UniGame.MetaBackend.Runtime
{
    using System;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UnityEngine;

    [Serializable]
    public class WebSpriteContract : IRemoteMetaContract, ILifeTimeContext
    {
        public string name = string.Empty;
        
        public ILifeTime lifeTime;
        
        public object Payload => name;
        public string Path => name;
        public Type OutputType => typeof(Sprite);
        public Type InputType => typeof(string);
        public Type FallbackType => typeof(object);
        public ILifeTime LifeTime => lifeTime;
    }
}