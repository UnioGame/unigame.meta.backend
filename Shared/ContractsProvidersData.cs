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
            backendTypes.RemoveAll(x => x.Provider == null || x.Id == 0);
            
            var providers = AssetEditorTools.GetAssets<BackendMetaServiceAsset>();
            var newProvider = new List<BackendMetaServiceAsset>();
            
            foreach (var provider in providers)
            {
                var foundProvider = backendTypes
                    .FirstOrDefault(x => x.Provider.GetType() == provider.GetType());
                if(foundProvider?.Provider!=null) continue;
                
                newProvider.Add(provider);
            }

            foreach (var serviceAsset in newProvider)
            {
                var backendName = serviceAsset.GetType().Name;
                backendTypes.Add(new BackendType()
                {
                    Name = backendName,
                    Provider = serviceAsset,
                    Id = backendName.GetHashCode(),
                });
            }
        }
#endif
        
    }
}