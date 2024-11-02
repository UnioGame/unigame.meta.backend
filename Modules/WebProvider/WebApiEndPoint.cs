namespace Game.Runtime.Services.WebService
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime.SerializableType;

#if UNITY_EDITOR
    using UniGame.MetaBackend.Shared;
    using UnityEditor;
#endif
    
    [Serializable]
    public class WebApiEndPoint : ISearchFilterable
    {
        public string name;
        public string url;
        public string path;
        public WebRequestType requestType = WebRequestType.Get;
        
        [ValueDropdown(nameof(GetContracts))]
        public SType contract = new();

        [BoxGroup("debug")]
        public DebugApiResult debugResult = new();

        public string Name => string.IsNullOrEmpty(name) ? path : name;
        
        public static IEnumerable<ValueDropdownItem<SType>> GetContracts()
        {
#if UNITY_EDITOR
            var types = TypeCache.GetTypesDerivedFrom(typeof(IRemoteMetaContract));
            foreach (var type in types)
            {
                if(type.IsAbstract || type.IsInterface) continue;
                
                yield return new ValueDropdownItem<SType>()
                {
                    Text = type.Name,
                    Value = type,
                };
            }
#endif
            yield break;
        }

        public bool IsMatch(string searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return true;
            if(name.Contains(searchString,StringComparison.OrdinalIgnoreCase)) return true;
            if(url.Contains(searchString,StringComparison.OrdinalIgnoreCase)) return true;
            
            if (contract != null && !string.IsNullOrEmpty(contract.TypeName))
            {
                var typeName = contract.TypeName;
                if(typeName.Contains(searchString,StringComparison.OrdinalIgnoreCase)) return true;
            }
            
            return false;
        }
    }

    [Serializable]
    public class DebugApiResult
    {
        [BoxGroup(nameof(data))]
        [MultiLineProperty(6)]
        [HideLabel]
        public string data = string.Empty;
        public bool success = true;
        public string error = string.Empty;
    }
    
    [Serializable]
    public enum WebRequestType : byte
    {
        Get,
        Post,
        //Put,
        //Delete,
    }
}