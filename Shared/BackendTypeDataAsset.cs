namespace UniGame.MetaBackend.Shared.Data
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Sirenix.OdinInspector;
    using UniModules;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Backend Type Data Asset")]
    public class BackendTypeDataAsset : ScriptableObject
    {
        [InlineProperty]
        public List<BackendType> Types = new();

        #region IdGenerator

#if UNITY_EDITOR
        private const string IdsType = "BackendTypeIds";
        private const string DefaultDirectory = "UniGame.Generated/RemoteMetaService/";
        private const string FileName = "BackendTypeIds.Generated.cs";

        [Button("Generate Static Properties")]
        public void GenerateProperties()
        {
            GenerateStaticProperties(this);
        }

        private static void GenerateStaticProperties(BackendTypeDataAsset dataAsset)
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

                var typesField = typeof(BackendTypeDataAsset).GetField("Types",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (typesField != null)
                {
                    var types = (List<BackendType>)typesField.GetValue(dataAsset);
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