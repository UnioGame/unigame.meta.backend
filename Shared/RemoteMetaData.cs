namespace Game.Modules.ModelMapping
{
    using System;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using Sirenix.OdinInspector;
    using UniModules.UniCore.Runtime.Utils;
    using UnityEngine;
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
        
        [ShowIf(nameof(overrideProvider))]
        public BackendTypeId provider = BackendTypeId.Empty;
        
        [BoxGroup(nameof(contract))]
        [HideLabel]
        [InlineProperty]
        [SerializeReference]
        public IRemoteMetaContract contract;
        
        [BoxGroup(nameof(converter))]
        public bool overriderDataConverter;
        
        [BoxGroup(nameof(converter))]
        [ShowIf(nameof(overriderDataConverter))]
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