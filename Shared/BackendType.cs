namespace UniGame.MetaBackend.Runtime
{
    using System;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public class BackendType
    {
        public string Name;
        public int Id;
        
#if ODIN_INSPECTOR
        [InlineEditor]
#endif
        public BackendMetaServiceAsset Provider;
    }
}