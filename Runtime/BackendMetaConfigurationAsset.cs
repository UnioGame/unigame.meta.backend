namespace MetaService.Runtime
{
    using System;
    using Game.Modules.ModelMapping;
    using Shared.Data;
    using Sirenix.OdinInspector;

    [Serializable]
    public class BackendMetaConfiguration
    {
        [BoxGroup("Settings")]
        [InlineProperty]
        [HideLabel]
        public BackendMetaSettings settings = new();

        [BoxGroup("Meta Data")]
        [HideLabel]
        [InlineEditor]
        public RemoteMetaDataConfigAsset metaDataAsset;
        
        [BoxGroup("Backend Type")]
        [InlineEditor]
        public BackendTypeDataAsset backend;
    }
}