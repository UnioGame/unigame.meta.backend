namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    public class TextureCacheItem : IDisposable
    {
        public string url;
        public string name;
        public Texture2D texture;
        public Sprite sprite;
        public int counter;
        public bool isAlive = true;
        public bool loaded;

        public void Dispose()
        {
            counter--;
            if(counter > 0) return;
            CleanUp();
        }

        public void CleanUp()
        {
            if(sprite) Object.Destroy(sprite);
            if(texture) Object.Destroy(texture);
            
            isAlive = false;
            name = string.Empty;
            url = string.Empty;
            loaded = false;
        }
    }
}