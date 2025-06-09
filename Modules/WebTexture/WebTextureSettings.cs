namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;

    [Serializable]
    public class WebTextureSettings
    {
        public string url = string.Empty;
        public bool useCache = true;
        
        [ListDrawerSettings(ListElementLabelName = "@Name")]
        public List<WebTexturePath> textures = new List<WebTexturePath>();
    }
}