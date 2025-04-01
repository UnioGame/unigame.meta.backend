using UnityEngine;
using UnityEditor;
using System;

namespace Game.Modules.unity.meta.service.Editor 
{
    public class RegenerateApi 
    {
        [MenuItem("Tools/WebApi/Regenerate API Contracts")]
        public static void Regenerate() 
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Content/Configuration/Backend/WebMetaProviderAsset.asset");
            System.Type type = asset.GetType();
            var settingsProperty = type.GetProperty("settings");
            var settings = settingsProperty.GetValue(asset);
            var apiSettingsProperty = settings.GetType().GetField("apiSettings");
            var apiSettings = apiSettingsProperty.GetValue(settings);
            var generateMethod = apiSettings.GetType().GetMethod("GenerateContracts");
            generateMethod.Invoke(apiSettings, null);
        }
    }
}
