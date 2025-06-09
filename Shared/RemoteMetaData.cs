namespace Game.Modules.ModelMapping
{
    using System;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UniGame.Runtime.Utils;
    using UnityEngine;
    
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
#if UNITY_EDITOR
    using UniModules.Editor;
#endif
    [Serializable]
    public class RemoteMetaData : ISearchFilterable
    {
        public static readonly RemoteMetaData Empty = new()
        {
            id = -1,
            enabled = false,
            contract = null,
            method = string.Empty,
            overriderDataConverter = false,
        };
        
#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [InlineButton(nameof(OpenScript),label:"Open Contract",icon:SdfIconType.Folder2Open)]
#endif
#if TRI_INSPECTOR || ODIN_INSPECTOR
        [GUIColor("GetButtonColor")]
#endif
#endif
        public bool enabled = true;
        public string method;
        public int id = -1;

        public bool overrideProvider = false;
        
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideProvider))]
#endif
        public BackendTypeId provider = BackendTypeId.Empty;
        
#if ODIN_INSPECTOR
        [BoxGroup(nameof(contract))]
        [HideLabel]
        [InlineProperty]
#endif
        [SerializeReference]
        public IRemoteMetaContract contract;
        
#if ODIN_INSPECTOR
        [BoxGroup(nameof(converter))]
#endif
        public bool overriderDataConverter;
        
#if ODIN_INSPECTOR
        [BoxGroup(nameof(converter))]
        [ShowIf(nameof(overriderDataConverter))]
#endif
        [SerializeReference]
        public IRemoteDataConverter converter;

        public bool IsMatch(string searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return true;
            
            var found = id.ToStringFromCache()
                            .Contains(searchString, StringComparison.OrdinalIgnoreCase) || 
                            method.Contains(searchString, StringComparison.OrdinalIgnoreCase);
            if (found) return true;
            
            var inputType = contract?.InputType;
            var outputType = contract?.OutputType;

            if (method != null && method.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            
            if(inputType!=null && inputType.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            
            if(outputType!=null && outputType.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            
            return false;
        }

#if UNITY_EDITOR
        
#if TRI_INSPECTOR
        [Button]
#endif
        public void OpenScript()
        {
            if(contract == null) return;
            contract.GetType().OpenScript();
        }
        
        private Color GetButtonColor()
        {
            return enabled ? 
                new Color(0.2f, 1f, 0.2f) : 
                new Color(1, 0.6f, 0.4f);
            return Color.green;
        }
        
#endif
    }
    
}