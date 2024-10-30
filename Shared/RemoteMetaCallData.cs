namespace Game.Modules.ModelMapping
{
    using System;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using Sirenix.OdinInspector;
    using UniModules.UniCore.Runtime.Utils;
    using UnityEngine;

    [Serializable]
    public class RemoteMetaCallData : ISearchFilterable
    {
        public static readonly RemoteMetaCallData Empty = new()
        {
            id = -1,
            enabled = false,
            contract = null,
            method = string.Empty,
            overriderDataConverter = false,
        };
        
        public bool enabled = true;
        public string method;
        [ShowIf(nameof(enabled))]
        public int id = 0;
        
        [ShowIf(nameof(enabled))]
        public BackendTypeId provider = BackendTypeId.Empty;
        
        [ShowIf(nameof(enabled))]
        [BoxGroup(nameof(contract))]
        [HideLabel]
        [InlineProperty]
        [SerializeReference]
        public IRemoteCallContract contract;
        
        [ShowIf(nameof(enabled))]
        [BoxGroup(nameof(converter))]
        public bool overriderDataConverter;
        
        [ShowIf(nameof(enabled))]
        [BoxGroup(nameof(converter))]
        [ShowIf(nameof(overriderDataConverter))]
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

    }
    
}