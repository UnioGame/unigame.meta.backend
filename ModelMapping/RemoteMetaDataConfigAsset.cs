namespace Game.Modules.ModelMapping
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using MetaService.Shared;
    using Sirenix.OdinInspector;
    using UniModules.Editor;
    using UniModules.UniCore.Runtime.Utils;
    using UnityEditor;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Meta Backend/Remote Meta Data Config", 
        fileName = "RemoteMetaDataConfiguration")]
    public class RemoteMetaDataConfigAsset : ScriptableObject
    {
        [HideLabel]
        [InlineProperty]
        public RemoteMetaDataConfig configuration = new();
        
        #region IdGenerator

#if UNITY_EDITOR
        
        [PropertyOrder(-1)]
        [Button(icon: SdfIconType.Hammer,"Remake Remote Meta Data")]
        public void FillRemoteMetaData()
        {
            var remoteItems = LoadRemoteMetaData();
            configuration.remoteMetaData = remoteItems
                .Values.ToArray();
            
            this.MarkDirty();
            
            AssetDatabase.SaveAssets();
        }
        
        [Button(icon: SdfIconType.ArrowClockwise,"Update Remote Meta Data")]
        [PropertyOrder(-1)]
        public void UpdateRemoteMetaData()
        {
            var remoteItems = LoadRemoteMetaData();
            var sourceItems = configuration
                .remoteMetaData
                .ToDictionary(x => x.id);

            foreach (var item in remoteItems)
            {
                if(sourceItems.ContainsKey(item.Key)) continue;
                sourceItems[item.Key] = item.Value;
            }
            
            configuration.remoteMetaData = sourceItems.Values.ToArray();
            
            this.MarkDirty();
            
            AssetDatabase.SaveAssets();
        }
        
        public Dictionary<int,RemoteMetaCallData> LoadRemoteMetaData()
        {
            var remoteCallContractType = typeof(IRemoteCallContract);
            var contractTypes = TypeCache.GetTypesDerivedFrom(remoteCallContractType);
            var remoteModels = new Dictionary<int,RemoteMetaCallData>();
            
            foreach (var typeItem in contractTypes)
            {
                if(!ValidateType(typeItem)) continue;

                var contract = typeItem.CreateWithDefaultConstructor() as IRemoteCallContract;
                if(contract == null) continue;
                
                var contractName = configuration.GetContractName(contract);
                var method = configuration.GetRemoteMethodName(contract);
                var id = configuration.CalculateMetaId(contractName, contract);
                
                var remoteItem = new RemoteMetaCallData()
                {
                    id = id,
                    name = contractName,
                    method = method,
                    contract = contract,
                    overriderDataConverter = false,
                    converter = configuration.defaultConverter,
                };

                remoteModels[id] = remoteItem;
            }

            return remoteModels;
        }

        public bool ValidateType(Type type)
        {
            if(type == null) return false;
            if(type.IsAbstract || type.IsInterface) return false;
            if(type.IsGenericType)  return false;
            if(type.HasDefaultConstructor() == false)  return false;
            
            return true;
        }
        
        public static void GenerateStaticProperties(RemoteMetaDataConfigAsset dataAsset)
        {
            var idType = typeof(RemoteMetaId);
            var typeName = nameof(RemoteMetaId);
            var outputPath = $"/UniGame.Generated/{typeName}/"
                .FixUnityPath()
                .ToProjectPath();
            
            var outputFileName = "RemoteMetaId.Generated.cs";

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var namespaceName = idType.Namespace;

            var filePath = outputPath.CombinePath(outputFileName);
            
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine($"namespace {namespaceName}");
                writer.WriteLine("{");
                writer.WriteLine($"    public partial struct {typeName}");
                writer.WriteLine("    {");

                var items = dataAsset.configuration.remoteMetaData;
                    
                foreach (var item in items)
                {
                    var name = item.name;
                    if(name == null) continue;
                    
                    var propertyName = name.Replace(" ", "");
                    writer.WriteLine(
                        $"        public static {typeName} {propertyName} = new {typeName} {{ value = {item.id} }};");
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