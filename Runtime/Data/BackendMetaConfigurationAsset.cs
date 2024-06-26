namespace MetaService.Runtime.Data
{
    using System;
    using Shared.Data;
    using Sirenix.OdinInspector;

    [Serializable]
    public class BackendMetaConfiguration
    {
        [InlineProperty]
        [HideLabel]
        public BackendMetaSettings settings = new();

        public RemoteMetaDataConfiguration
        
        [InlineEditor]
        public BackendTypeDataAsset data;
    }
}