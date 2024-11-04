namespace Modules.WebTexture
{
    using System;

    [Serializable]
    public class WebTexturePath
    {
        public string name;
        public string url;
        
        public string Name => string.IsNullOrEmpty(name) ? url : name;
    }
}