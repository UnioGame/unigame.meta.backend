namespace MetaService.Shared.Data
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Sirenix.OdinInspector;
    using UnityEngine;

#if UNITY_EDITOR
    using UniModules.Editor;
    using UnityEditor;
#endif
    
    [CreateAssetMenu(menuName = "UniGame/Meta Backend/Backend Type Data Asset", fileName = "Backend Type Data Asset")]
    public class BackendTypeDataAsset : ScriptableObject
    {
        public const string DefaultDirectory = "UniGame.Generated/RemoteMetaService/";
        public const string FileName = "BackendTypeIds.Generated.cs";
        
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
                writer.WriteLine("    public struct BackendTypeIds");
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

            AssetDatabase.Refresh();
            Debug.Log("Partial class with static properties generated successfully.");
        }

#endif

        #endregion

    }
}