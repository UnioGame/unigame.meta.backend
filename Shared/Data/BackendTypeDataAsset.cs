namespace MetaService.Shared.Data
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Sirenix.OdinInspector;
    using UnityEditor;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Meta Backend/BackendType Data Asset", fileName = "BackendType  Data Asset")]
    public class BackendTypeDataAsset : ScriptableObject
    {
        [InlineProperty]
        public List<BackendType> Types = new List<BackendType>();

        #region IdGenerator

#if UNITY_EDITOR
        [Button("Generate Static Properties")]
        public void GenerateProperties()
        {
            GenerateStaticProperties(this);
        }

        public static void GenerateStaticProperties(BackendTypeDataAsset dataAsset)
        {
            var idType = typeof(BackendTypeId);
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(dataAsset));
            var directoryPath = Path.GetDirectoryName(scriptPath);
            var outputPath = Path.Combine(directoryPath, "Generated");
            var outputFileName = "BackendTypeId.Generated.cs";

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
                writer.WriteLine("    public partial struct BackendTypeId");
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
                            $"        public static BackendTypeId {propertyName} => new BackendTypeId {{ value = {type.Id} }};");
                    }
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