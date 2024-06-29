namespace Game.Modules.ModelMapping
{
    using System;
    using MetaService.Shared;
    using Sirenix.OdinInspector;
    using UniModules.UniCore.Runtime.Utils;
    using UnityEngine;

    [Serializable]
    public class RemoteMetaCallData : ISearchFilterable
    {
        public static readonly RemoteMetaCallData Empty = new()
        {
            id = -1,
            contract = null,
            name = string.Empty,
            overriderDataConverter = false,
        };
        
        public string name;
        public int id = 0;
        public string method;
        
        [BoxGroup(nameof(contract))]
        [HideLabel]
        [InlineProperty]
        [SerializeReference]
        public IRemoteCallContract contract;
        
        [BoxGroup(nameof(converter))]
        public bool overriderDataConverter;
        
        [BoxGroup(nameof(converter))]
        [ShowIf(nameof(overriderDataConverter))]
        public IRemoteDataConverter converter;

        public bool IsMatch(string searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return true;
            
            var found = id.ToStringFromCache()
                            .Contains(searchString, StringComparison.OrdinalIgnoreCase) || 
                        name.Contains(searchString, StringComparison.OrdinalIgnoreCase);
            if (found) return true;
            
            var inputType = contract?.InputType;
            var outputType = contract?.OutputType;
            var method = contract?.MethodName;

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