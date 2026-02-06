namespace MetaService.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UniGame.MetaBackend.Runtime;
    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
#if UNITY_EDITOR
    using UniModules.Editor;
#endif
    
    [Serializable]
    public class ContractsProvidersData
    {
        public bool enableLogging = false;
        
        public bool useDefaultBackendFirst = true;
        
        [Tooltip("Number of historical records to keep for backend requests")]
        public int historySize = 100;
        
        public BackendTypeId backendType;
        
#if ODIN_INSPECTOR
        [InlineProperty]
#endif
        public List<BackendType> backendTypes = new();
        
#if UNITY_EDITOR

#if ODIN_INSPECTOR
        [Button("Load Providers")]
#endif
        public void LoadProviders()
        {
            backendTypes.RemoveAll(x => x.provider == null || x.id == 0);
            
            var providers = AssetEditorTools.GetAssets<BackendMetaServiceAsset>();
            var newProvider = new List<BackendMetaServiceAsset>();
            
            foreach (var provider in providers)
            {
                var foundProvider = backendTypes
                    .FirstOrDefault(x => x.provider == provider);
                
                if(foundProvider?.provider!=null) continue;
                
                newProvider.Add(provider);
            }

            foreach (var serviceAsset in newProvider)
            {
                backendTypes.Add(new BackendType()
                {
                    name = serviceAsset.name,
                    provider = serviceAsset,
                    id = serviceAsset.name.GetHashCode(),
                });
            }

            foreach (var contract in backendTypes)
            {
                if (!string.IsNullOrEmpty(contract.name) && contract.id != 0)
                    continue;
                var serviceAsset = contract.provider;
                contract.name = serviceAsset.name;
                contract.provider = serviceAsset;
                contract.id = contract.id != 0 ? contract.id : serviceAsset.name.GetHashCode();
            }
        }
#endif
        
    }
}