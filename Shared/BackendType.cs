namespace UniGame.MetaBackend.Runtime
{
    using System;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public struct BackendType
    {
        public string Name;
        
        public int Id => string.IsNullOrEmpty(Name) ? 0 : Name.GetHashCode();

#if ODIN_INSPECTOR
        [InlineEditor]
#endif
        public BackendMetaServiceAsset Provider;
    }
}