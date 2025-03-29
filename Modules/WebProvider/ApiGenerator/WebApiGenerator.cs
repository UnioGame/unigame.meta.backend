using UnityEngine;

namespace Game.Modules.unity.meta.service.Modules.WebProvider
{
    /// <summary>
    /// Facade class for generating API contracts from Swagger JSON
    /// </summary>
    public static class WebApiGenerator
    {
        /// <summary>
        /// Generate API contracts from the configured Swagger JSON file
        /// </summary>
        /// <param name="settings">Web API settings containing configuration</param>
        public static void GenerateContracts(WebApiSettings settings)
        {
            Debug.Log("Starting API contract generation...");
            
            var generator = new SwaggerContractGenerator(settings);
            generator.GenerateContracts();
            
            Debug.Log("API contract generation completed.");
        }
    }
}