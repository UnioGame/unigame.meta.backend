namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public class WebTextureSettings
    {
        public string url = string.Empty;
        public bool useCache = true;
        
#if ODIN_INSPECTOR
        [ListDrawerSettings(ListElementLabelName = "@Name")]
#endif
        public List<WebTexturePath> textures = new List<WebTexturePath>();
    }
}