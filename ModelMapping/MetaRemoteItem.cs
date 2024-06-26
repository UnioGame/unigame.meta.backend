namespace Game.Modules.ModelMapping
{
    using System;
    using System.Collections.Generic;
    using MetaService.Shared;
    using MetaService.Shared.Attributes;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime.SerializableType;
    using UniModules.UniCore.Runtime.Utils;
    using UnityEditor;

    [Serializable]
    public class MetaRemoteItem : ISearchFilterable
    {
        public string getMethod = string.Empty;
        public string postMethod = string.Empty;
        public int id = 0;
        
        [ValueDropdown(nameof(GetModelTypes))]
        public SType type = new();

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
                        getMethod.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        postMethod.Contains(searchString, StringComparison.OrdinalIgnoreCase);
            
            found = found || 
                    (!string.IsNullOrEmpty(type?.fullTypeName) && 
                     type?.fullTypeName.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true);
            
            return found;
        }
        
        public static IEnumerable<SType> GetModelTypes()
        {
#if UNITY_EDITOR
            var targetTypesTypeCache = TypeCache
                .GetTypesWithAttribute(typeof(RemoteMetaModelAttribute));
            foreach (var target in targetTypesTypeCache)
            {
                if(target.IsAbstract || target.IsInterface) continue;
                yield return (SType)target;
            }

            var interfaceTypes = TypeCache.GetTypesDerivedFrom<IRemoteMetaModel>();
            foreach (var target in interfaceTypes)
            {
                if(target.IsAbstract || target.IsInterface) continue;
                yield return (SType)target;
            }
#endif
        }
    }
}