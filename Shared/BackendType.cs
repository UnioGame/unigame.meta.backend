namespace UniGame.MetaBackend.Shared.Data
{
    using System;
    using Sirenix.OdinInspector;

    [Serializable]
    public struct BackendType
    {
        public string Name;
        
        public int Id => string.IsNullOrEmpty(Name) ? 0 : Name.GetHashCode();

        [InlineEditor]
        public BackendMetaServiceAsset Provider;
    }
}