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
    public class RemoteMetaData : ISearchFilterable
    {
        private static HashSet<Type> _parameterTypes = new HashSet<Type>();
        
        public static readonly RemoteMetaData Empty = new()
        {
            id = -1,
            method = String.Empty,
            name = string.Empty,
            overriderDataConverter = false,
        };
        
        public string name;
        public string method = string.Empty;
        public int id = 0;
        
        [ValueDropdown(nameof(GetModelTypes))]
        public SType result = new();

        [ValueDropdown(nameof(GetParameterTypes))]
        public SType parameter = typeof(EmptyParameterData);
        
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
                        name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        method.Contains(searchString, StringComparison.OrdinalIgnoreCase);
            
            found = found || 
                    (!string.IsNullOrEmpty(result?.fullTypeName) && 
                     result?.fullTypeName.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true);
            
            return found;
        }
        
        public static IEnumerable<SType> GetModelTypes()
        {
#if UNITY_EDITOR
            _parameterTypes.Clear();
            
            var targetTypesTypeCache = TypeCache
                .GetTypesWithAttribute(typeof(RemoteMetaModelAttribute));
            
            foreach (var target in targetTypesTypeCache)
            {
                if(target.IsAbstract || target.IsInterface) continue;
                _parameterTypes.Add(target);
            }

            var interfaceTypes = TypeCache.GetTypesDerivedFrom<IRemoteMetaModel>();
            foreach (var target in interfaceTypes)
            {
                if(target.IsAbstract || target.IsInterface) continue;
                _parameterTypes.Add(target);
            }
            
            foreach (var type in _parameterTypes)
                yield return type;
#endif
        }
        
        
        public static IEnumerable<SType> GetParameterTypes()
        {
#if UNITY_EDITOR
            _parameterTypes.Clear();
            var targetTypesTypeCache = TypeCache.GetTypesWithAttribute(typeof(RemoteMetaParameterAttribute));
            
            foreach (var target in targetTypesTypeCache)
            {
                if(target.IsAbstract || target.IsInterface) continue;
                _parameterTypes.Add(target);
            }

            var interfaceTypes = TypeCache.GetTypesDerivedFrom<IRemoteMetaParameter>();
            foreach (var target in interfaceTypes)
            {
                if(target.IsAbstract || target.IsInterface) continue;
                _parameterTypes.Add(target);
            }

            foreach (var type in _parameterTypes)
                yield return type;
#endif
        }
    }
}