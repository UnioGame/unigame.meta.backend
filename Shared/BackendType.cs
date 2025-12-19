namespace UniGame.MetaBackend.Runtime
{
    using System;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public class BackendType
    {
        public string name;
        public int id;
        public bool isEnabled = true;
        
#if ODIN_INSPECTOR
        [InlineEditor]
#endif
        public BackendMetaServiceAsset provider;
    }
}