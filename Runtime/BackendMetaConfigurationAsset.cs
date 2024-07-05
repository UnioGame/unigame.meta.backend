namespace MetaService.Runtime
{
    using System;
    using Game.Modules.ModelMapping;
    using Shared.Data;
    using Sirenix.OdinInspector;

    [Serializable]
    public class BackendMetaConfiguration
    {
        [BoxGroup("Meta Data")]
        [HideLabel]
        [InlineEditor]
        public RemoteMetaDataConfigAsset meta;

        [BoxGroup("Backend Type")]
        [InlineEditor]
        public BackendTypeDataAsset backend;
    }
}