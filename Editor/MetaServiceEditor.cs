namespace Game.Modules.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using MetaService.Runtime;
    using ModelMapping;
    using UniGame.MetaBackend.Shared.Data;
    using UniModules;
    using UniModules.Editor;
    using UnityEditor;
    using UnityEngine;

    public class MetaServiceEditor
    {
        [MenuItem("Assets/UniGame/Meta Service/Create Configuration")]
        public static void CreateViewSystemPrefab()
        {
            var activeObject = Selection.activeObject;
            if (!activeObject) return;
            
            var path = AssetDatabase.GetAssetPath(activeObject);
            path = path.GetDirectoryPath();

            Debug.Log($"ASSET PATH SELECTION :  {path}");
            
            CreateConfiguration(path);
        }
        
        public static void CreateConfiguration(string path)
        {
            var source = ScriptableObject.CreateInstance<BackendMetaSource>();
            source = source.SaveAsset(source.GetType().Name,path);
            
            var metaDataConfig = ScriptableObject.CreateInstance<RemoteMetaDataConfigAsset>();
            metaDataConfig = metaDataConfig.SaveAsset(metaDataConfig.GetType().Name,path);
            
            var typeDataAsset = ScriptableObject.CreateInstance<BackendTypeDataAsset>();
            typeDataAsset = typeDataAsset.SaveAsset(typeDataAsset.GetType().Name,path);

            source.backendMetaConfiguration = new BackendMetaConfiguration()
            {
                backend = typeDataAsset,
                meta = metaDataConfig,
            };

            source.MarkDirty();
            
            var providersTypes = TypeCache.GetTypesDerivedFrom<BackendMetaServiceAsset>();

            var providers = new List<BackendMetaServiceAsset>();
            foreach (var type in providersTypes)
            {
                if(type.IsAbstract || type.IsInterface) continue;
                var provider = ScriptableObject.CreateInstance(type) as BackendMetaServiceAsset;
                var providerName = type.Name;
                provider = provider.SaveAsset(providerName,path);
                providers.Add(provider);
            }
            
            typeDataAsset.Types = providers
                .Select(x => new BackendType()
                {
                    Name = x.GetType().Name,
                    Provider = x
                }).ToList();
            
            typeDataAsset.MarkDirty();
                
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
