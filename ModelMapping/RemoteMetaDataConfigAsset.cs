namespace Game.Modules.ModelMapping
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Sirenix.OdinInspector;
    using UniModules.Editor;
    using UnityEditor;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Game/Services/Meta Backend/Remote Meta Data Config", 
        fileName = "RemoteMetaDataConfiguration")]
    public class RemoteMetaDataConfigAsset : ScriptableObject
    {
        [HideLabel]
        [InlineProperty]
        public RemoteMetaDataConfig configuration = new();
        
        #region IdGenerator

#if UNITY_EDITOR

        [Button("Fill Remote Meta Data")]
        public void FillRemoteMetaData()
        {
            var remoteItems = LoadRemoteMetaData();
            configuration.remoteMetaData = remoteItems.ToArray();
            
            this.MarkDirty();
            
            AssetDatabase.SaveAssets();
            GenerateProperties();
        }
        
        [Button("Update Remote Meta Data")]
        public void UpdateRemoteMetaData()
        {
            var remoteItems = LoadRemoteMetaData();
            var sourceItems = configuration.remoteMetaData.ToList();
            var resultItems = new List<MetaRemoteItem>(sourceItems);

            foreach (var item in remoteItems)
            {
                var sourceItem = sourceItems.FirstOrDefault(x => x.id == item.id);
                if(sourceItem!=null) continue;
                resultItems.Add(item);
            }
            
            configuration.remoteMetaData = resultItems.ToArray();
            
            this.MarkDirty();
            
            AssetDatabase.SaveAssets();
            GenerateProperties();
        }
        
        public List<MetaRemoteItem> LoadRemoteMetaData()
        {
            var remoteModels = new List<MetaRemoteItem>();
            var counter = 0;
            
            foreach (var typeItem in MetaRemoteItem.GetModelTypes())
            {
                var type = (Type)typeItem;
                if (type == null) continue;
                var typeName = type.Name;

                var remoteItem = new MetaRemoteItem()
                {
                    id = counter++,
                    type = typeItem,
                    overriderDataConverter = false,
                    getMethod = string.Format(configuration.getMethodTemplate, typeName),
                    postMethod = string.Format(configuration.postMethodTemplate, typeName),
                    converter = new JsonRemoteDataConverter(),
                };
                
                remoteModels.Add(remoteItem);
            }

            return remoteModels;
        }
        
        [Button("Generate Static Properties")]
        public void GenerateProperties()
        {
            GenerateStaticProperties(this);
        }

        public static void GenerateStaticProperties(RemoteMetaDataConfigAsset dataAsset)
        {
            var idType = typeof(RemoteMetaId);
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(dataAsset));
            var directoryPath = Path.GetDirectoryName(scriptPath);
            var outputPath = Path.Combine(directoryPath, "Generated");
            var outputFileName = "RemoteMetaId.Generated.cs";

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var namespaceName = idType.Namespace;

            var filePath = Path.Combine(outputPath, outputFileName);
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine($"namespace {namespaceName}");
                writer.WriteLine("{");
                writer.WriteLine("    public partial struct RemoteMetaId");
                writer.WriteLine("    {");
                dataAsset
                var types = (List<BackendType>)typesField.GetValue(dataAsset);
                foreach (var type in types)
                {
                    var propertyName = type.Name.Replace(" ", "");
                    writer.WriteLine(
                        $"        public static BackendTypeId {propertyName} => new BackendTypeId {{ value = {type.Id} }};");
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            AssetDatabase.Refresh();
            Debug.Log("Partial class with static properties generated successfully.");
        }

#endif

        #endregion
    }
}