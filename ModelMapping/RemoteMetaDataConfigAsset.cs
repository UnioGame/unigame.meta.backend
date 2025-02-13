namespace Game.Modules.ModelMapping
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using MetaService.Runtime;
    using NUnit.Framework;
    using UniGame.MetaBackend.Shared;
    using Sirenix.OdinInspector;
    using UniModules;
    using UniModules.UniCore.Runtime.Utils;
    using UnityEngine;

#if UNITY_EDITOR
    using UniModules.Editor;
    using UnityEditor;
    using UnityEngine.UIElements;
#endif

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Remote Meta Data Config")]
    public class RemoteMetaDataConfigAsset : ScriptableObject
    {
        [PropertyOrder(-1)]
        [BoxGroup(nameof(settings))]
        [InlineProperty]
        [HideLabel]
        public BackendMetaSettings settings = new();

        [BoxGroup(nameof(settings))]
        [HideLabel]
        [InlineProperty]
        public RemoteMetaDataConfig configuration = new();

        #region IdGenerator

#if UNITY_EDITOR

        [PropertyOrder(-1)]
        [Button(icon: SdfIconType.ImageFill)]
        [ButtonGroup("Providers")]
        public void SetDefaultTypeForAll()
        {
            foreach (var metaCallData in configuration.remoteMetaData)
            {
                metaCallData.provider = settings.backendType;
            }
        }

        [PropertyOrder(-1)]
        [Button(icon: SdfIconType.Hammer)]
        [ButtonGroup("Providers")]
        public void FillRemoteMetaData()
        {
            var remoteItems = LoadRemoteMetaData();

            var newMetaDatas = new List<RemoteMetaData>();
            var itemForUpdate = new List<RemoteMetaData>();

            foreach (var remoteItem in remoteItems.Values)
            {
                var contains = false;
                
                foreach (var data in configuration.remoteMetaData)
                {
                    if ($"{remoteItem.method}Contract" != data.method) continue;
                    
                    contains = true;
                    break;
                }

                if (!contains)
                {
                    itemForUpdate.Add(remoteItem);
                }
            }

            newMetaDatas = configuration.remoteMetaData.ToList();
            newMetaDatas.AddRange(itemForUpdate);
            configuration.remoteMetaData = newMetaDatas.ToArray();
            UpdateRemoteMetas(itemForUpdate);

            this.MarkDirty();

            AssetDatabase.SaveAssets();
        }

        [Button(icon: SdfIconType.ArrowClockwise, "Update Remote Meta Data")]
        [PropertyOrder(-1)]
        public void UpdateRemoteMetaData()
        {
            var remoteItems = LoadRemoteMetaData();
            var sourceItems = configuration
                .remoteMetaData
                .ToDictionary(x => x.id);

            foreach (var item in remoteItems)
            {
                if (sourceItems.ContainsKey(item.Key)) continue;
                sourceItems[item.Key] = item.Value;
            }

            configuration.remoteMetaData = sourceItems.Values.ToArray();

            UpdateRemoteMetas(configuration.remoteMetaData.ToList());

            this.MarkDirty();

            AssetDatabase.SaveAssets();
        }

        private void UpdateRemoteMetas(List<RemoteMetaData> datas)
        {
            foreach (var metaCallData in datas)
            {
                var method = metaCallData.contract.MethodName;
                metaCallData.method = string.IsNullOrEmpty(method)
                    ? metaCallData.contract.GetType().Name
                    : method;
            }
        }

        public Dictionary<int, RemoteMetaData> LoadRemoteMetaData()
        {
            var remoteCallContractType = typeof(IRemoteMetaContract);
            var contractTypes = TypeCache.GetTypesDerivedFrom(remoteCallContractType);
            var remoteModels = new Dictionary<int, RemoteMetaData>();

            foreach (var typeItem in contractTypes)
            {
                if (!ValidateType(typeItem)) continue;

                var contract = typeItem.CreateWithDefaultConstructor() as IRemoteMetaContract;
                if (contract == null) continue;

                var method = configuration.GetRemoteMethodName(contract);
                var id = configuration.CalculateMetaId(contract);

                var remoteItem = new RemoteMetaData()
                {
                    id = id,
                    method = method,
                    contract = contract,
                    provider = settings.backendType,
                    overriderDataConverter = false,
                    converter = configuration.defaultConverter,
                };

                remoteModels[id] = remoteItem;
            }

            return remoteModels;
        }

        public bool ValidateType(Type type)
        {
            if (type == null) return false;
            if (type.IsAbstract || type.IsInterface) return false;
            if (type.IsGenericType) return false;
            if (type.HasDefaultConstructor() == false) return false;

            return true;
        }

        [PropertyOrder(-1)]
        [Button("Generate Static Properties")]
        public void GenerateProperties()
        {
            GenerateStaticProperties(this);
        }

        public static void GenerateStaticProperties(RemoteMetaDataConfigAsset dataAsset)
        {
            var idType = typeof(RemoteMetaId);
            var typeName = nameof(RemoteMetaId);
            var outputPath = $"/UniGame.Generated/RemoteMetaService/"
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
                writer.WriteLine($"    public struct RemoteMetaContracts");
                writer.WriteLine("    {");

                var items = dataAsset.configuration.remoteMetaData;

                foreach (var item in items)
                {
                    var name = item.method;
                    if (name == null) continue;

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