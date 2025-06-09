namespace MetaService.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UniGame.MetaBackend.Runtime;
    using UniModules;
    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
#if UNITY_EDITOR
    using UniModules.Editor;
#endif
    
    [Serializable]
    public class BackendMetaSettings
    {
        private const string IdsType = "BackendTypeIds";
        private const string DefaultDirectory = "UniGame.Generated/RemoteMetaService/";
        private const string FileName = "BackendTypeIds.Generated.cs";
        
        public BackendTypeId backendType;
        
#if ODIN_INSPECTOR
        [InlineProperty]
#endif
        public List<BackendType> backendTypes = new();
        
#region IdGenerator
#if UNITY_EDITOR

#if ODIN_INSPECTOR
        [Button("Load Providers")]
#endif
        public void LoadProviders()
        {
            var providers = AssetEditorTools.GetAssets<BackendMetaServiceAsset>();
            var newProvider = new List<BackendMetaServiceAsset>();
            
            foreach (var provider in providers)
            {
                var foundProvider = backendTypes
                    .FirstOrDefault(x => x.Provider.GetType() == provider.GetType());
                if(foundProvider.Provider!=null) continue;
                
                newProvider.Add(provider);
            }

            foreach (var serviceAsset in newProvider)
            {
                backendTypes.Add(new BackendType()
                {
                    Name = serviceAsset.GetType().Name,
                    Provider = serviceAsset,
                });
            }
        }
        
#if ODIN_INSPECTOR
        [Button("Generate Static Properties")]
#endif
        public void GenerateProperties()
        {
            GenerateStaticProperties(this);
        }

        private static void GenerateStaticProperties(BackendMetaSettings dataAsset)
        {
            var idType = typeof(BackendTypeId);
            var idsTypeName = IdsType;
            var outputPath = DefaultDirectory
                .ToProjectPath()
                .FixUnityPath();

            var filePath = outputPath.CombinePath(FileName);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var namespaceName = idType.Namespace;

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine($"namespace {namespaceName}");
                writer.WriteLine("{");
                writer.WriteLine($"    public struct {idsTypeName}");
                writer.WriteLine("    {");

                var typesField = dataAsset.backendTypes;
                
                if (typesField != null)
                {
                    var types = typesField;
                    foreach (var type in types)
                    {
                        var propertyName = type.Name.Replace(" ", "");
                        writer.WriteLine(
                            $"        public static {idType} {propertyName} = new {idType} {{ value = {type.Id} }};");
                    }
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("Partial class with static properties generated successfully.");
        }

#endif
#endregion
    }
}