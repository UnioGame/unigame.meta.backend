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

        [InlineEditor]
        public BackendTypeDataAsset data;
    }
}