namespace Game.Modules.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using MetaService.Runtime;
    using ModelMapping;
    using UniGame.MetaBackend.Runtime;
    using UniModules;
    using UniModules.Editor;
    using UnityEditor;
    using UnityEngine;

    public class MetaServiceEditor
    {
        [MenuItem("Assets/UniGame/Meta Service/Create Configuration")]
        public static void CreateMetaServiceConfiguration()
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
            
            var metaDataConfig = ScriptableObject.CreateInstance<ContractsConfigurationAsset>();
            metaDataConfig = metaDataConfig.SaveAsset(metaDataConfig.GetType().Name,path);

            source.configuration = metaDataConfig;
            
            metaDataConfig.MarkDirty();
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
            
            metaDataConfig.settings.backendTypes = providers
                .Select(x => new BackendType()
                {
                    Name = x.GetType().Name,
                    Id = x.GetType().Name.GetHashCode(),
                    Provider = x
                }).ToList();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
