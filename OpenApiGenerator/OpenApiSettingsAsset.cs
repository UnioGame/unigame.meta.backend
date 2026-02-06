namespace Game.Modules.unity.meta.service.Modules.WebProvider
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/MetaBackend/Open Api Settings",fileName =  "OpenApiSettings")]
    public class OpenApiSettingsAsset : ScriptableObject
    {
#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        public OpenApiSettings apiSettings = new OpenApiSettings();
    }
}