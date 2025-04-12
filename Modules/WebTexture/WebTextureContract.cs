namespace Modules.WebTexture
{
    using System;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UnityEngine;

    [Serializable]
    public class WebTexture2DContract : IRemoteMetaContract,ILifeTimeContext
    {
        public string name = string.Empty;
        public ILifeTime lifeTime;
        
        public object Payload => name;
        public string Path => name;
        public Type OutputType => typeof(Texture2D);
        public Type InputType => typeof(string);
        public Type FallbackType => typeof(object);
        public ILifeTime LifeTime => lifeTime;
        
    }
}