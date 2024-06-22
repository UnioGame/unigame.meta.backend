namespace MetaService.Runtime.Data
{
    using Shared.Data;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Game/Services/Meta Backend/Backend Meta Settings", fileName = "Backend Meta Settings")]
    public class BackendMetaConfigurationAsset : ScriptableObject
    {
        [InlineProperty]
        [HideLabel]
        public BackendMetaSettings settings = new();

        [InlineEditor]
        public BackendTypeDataAsset data;
    }
}