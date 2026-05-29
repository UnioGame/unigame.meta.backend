namespace UniGame.MetaBackend.Runtime
{
    using System;
    using UnityEngine.Serialization;

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
        public void Normalize()
        {
            if (provider == null) return;

            if (string.IsNullOrEmpty(name))
                name = provider.name;

            if (id == 0 && !string.IsNullOrEmpty(name))
                id = name.GetHashCode();
        }
    }
}