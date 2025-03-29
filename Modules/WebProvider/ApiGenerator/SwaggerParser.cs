using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Modules.unity.meta.service.Modules.WebProvider
{
    /// <summary>
    /// Parses Swagger JSON to extract API definitions
    /// </summary>
    public class SwaggerParser
    {
        public SwaggerApiDefinition Parse(string jsonContent)
        {
            try
            {
                var definition = new SwaggerApiDefinition();
                var jsonObject = JObject.Parse(jsonContent);

                // Parse basic info
                definition.Title = jsonObject["info"]?["title"]?.ToString();
                definition.Version = jsonObject["info"]?["version"]?.ToString();
                definition.BasePath = jsonObject["basePath"]?.ToString();

                // Parse paths
                var paths = jsonObject["paths"] as JObject;
                if (paths != null)
                {
                    definition.Paths = ParsePaths(paths);
                }

                // Parse definitions (schemas) - Swagger 2.0
                var definitions = jsonObject["definitions"] as JObject;
                if (definitions != null)
                {
                    definition.Definitions = ParseDefinitions(definitions);
                }
                
                // Parse components.schemas (OpenAPI 3.0)
                var components = jsonObject["components"] as JObject;
                if (components != null)
                {
                    var schemas = components["schemas"] as JObject;
                    if (schemas != null)
                    {
                        // Если определения уже есть, добавим новые
                        if (definition.Definitions == null)
                        {
                            definition.Definitions = ParseDefinitions(schemas);
                        }
                        else
                        {
                            var componentDefinitions = ParseDefinitions(schemas);
                            foreach (var kvp in componentDefinitions)
                            {
                                definition.Definitions[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }

                return definition;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing Swagger JSON: {ex.Message}");
                return new SwaggerApiDefinition();
            }
        }

        private Dictionary<string, SwaggerPathItem> ParsePaths(JObject paths)
        {
            var result = new Dictionary<string, SwaggerPathItem>();

            foreach (var pathProperty in paths.Properties())
            {
                string pathUrl = pathProperty.Name;
                var pathItem = new SwaggerPathItem();

                var operations = pathProperty.Value as JObject;
                if (operations != null)
                {
                    pathItem.Methods = ParseMethods(operations);
                }

                result.Add(pathUrl, pathItem);
            }

            return result;
        }

        private Dictionary<string, SwaggerOperation> ParseMethods(JObject operations)
        {
            var result = new Dictionary<string, SwaggerOperation>();

            foreach (var operationProperty in operations.Properties())
            {
                string method = operationProperty.Name.ToLower();
                var operation = new SwaggerOperation();

                var operationObj = operationProperty.Value as JObject;
                if (operationObj != null)
                {
                    operation.OperationId = operationObj["operationId"]?.ToString();
                    operation.Summary = operationObj["summary"]?.ToString();
                    operation.Description = operationObj["description"]?.ToString();
                    operation.Tags = operationObj["tags"]?.ToObject<List<string>>();

                    // Parse parameters
                    var parameters = operationObj["parameters"] as JArray;
                    if (parameters != null)
                    {
                        operation.Parameters = ParseParameters(parameters);
                    }

                    // Parse responses
                    var responses = operationObj["responses"] as JObject;
                    if (responses != null)
                    {
                        operation.Responses = ParseResponses(responses);
                    }
                }

                result.Add(method, operation);
            }

            return result;
        }

        private List<SwaggerParameter> ParseParameters(JArray parameters)
        {
            var result = new List<SwaggerParameter>();

            foreach (var paramToken in parameters)
            {
                var param = new SwaggerParameter();
                var paramObj = paramToken as JObject;
                
                if (paramObj != null)
                {
                    param.Name = paramObj["name"]?.ToString();
                    param.In = paramObj["in"]?.ToString();
                    param.Description = paramObj["description"]?.ToString();
                    param.Required = paramObj["required"]?.ToObject<bool>() ?? false;
                    param.Type = paramObj["type"]?.ToString();
                    
                    // Handle schema reference
                    var schemaObj = paramObj["schema"] as JObject;
                    if (schemaObj != null)
                    {
                        param.Schema = ParseSchema(schemaObj);
                    }
                }

                result.Add(param);
            }

            return result;
        }

        private Dictionary<string, SwaggerResponse> ParseResponses(JObject responses)
        {
            var result = new Dictionary<string, SwaggerResponse>();

            foreach (var responseProperty in responses.Properties())
            {
                string statusCode = responseProperty.Name;
                var response = new SwaggerResponse();

                var responseObj = responseProperty.Value as JObject;
                if (responseObj != null)
                {
                    response.Description = responseObj["description"]?.ToString();
                    
                    // Пробуем получить схему напрямую (Swagger 2.0)
                    var schema = responseObj["schema"] as JObject;
                    if (schema != null)
                    {
                        response.Schema = ParseSchema(schema);
                    }
                    else
                    {
                        // Пробуем получить схему из content (OpenAPI 3.0)
                        var content = responseObj["content"] as JObject;
                        if (content != null)
                        {
                            // Ищем application/json
                            var jsonContent = content["application/json"] as JObject;
                            if (jsonContent != null)
                            {
                                var contentSchema = jsonContent["schema"] as JObject;
                                if (contentSchema != null)
                                {
                                    response.Schema = ParseSchema(contentSchema);
                                }
                            }
                        }
                    }
                }

                result.Add(statusCode, response);
            }

            return result;
        }

        private SwaggerSchema ParseSchema(JObject schema)
        {
            var result = new SwaggerSchema();

            // Handle $ref
            var reference = schema["$ref"]?.ToString();
            if (!string.IsNullOrEmpty(reference))
            {
                result.Reference = NormalizeReference(reference);
                return result;
            }

            // Handle array type
            if (schema["type"]?.ToString() == "array")
            {
                result.Type = "array";
                var items = schema["items"] as JObject;
                if (items != null)
                {
                    result.Items = ParseSchema(items);
                }
                return result;
            }

            // Handle object type
            if (schema["type"]?.ToString() == "object")
            {
                result.Type = "object";
                var properties = schema["properties"] as JObject;
                if (properties != null)
                {
                    result.Properties = new Dictionary<string, SwaggerSchema>();
                    foreach (var property in properties.Properties())
                    {
                        var propObj = property.Value as JObject;
                        if (propObj != null)
                        {
                            result.Properties.Add(property.Name, ParseSchema(propObj));
                        }
                    }
                }
                return result;
            }

            // Handle primitive types
            result.Type = schema["type"]?.ToString();
            result.Format = schema["format"]?.ToString();
            
            return result;
        }

        private Dictionary<string, SwaggerDefinition> ParseDefinitions(JObject definitions)
        {
            var result = new Dictionary<string, SwaggerDefinition>();

            foreach (var definitionProperty in definitions.Properties())
            {
                string name = definitionProperty.Name;
                var definition = new SwaggerDefinition();

                var definitionObj = definitionProperty.Value as JObject;
                if (definitionObj != null)
                {
                    definition.Type = definitionObj["type"]?.ToString();
                    definition.Title = definitionObj["title"]?.ToString();
                    
                    // Parse properties
                    var properties = definitionObj["properties"] as JObject;
                    if (properties != null)
                    {
                        definition.Properties = new Dictionary<string, SwaggerProperty>();
                        
                        foreach (var property in properties.Properties())
                        {
                            var prop = new SwaggerProperty();
                            var propObj = property.Value as JObject;
                            
                            if (propObj != null)
                            {
                                prop.Type = propObj["type"]?.ToString();
                                prop.Format = propObj["format"]?.ToString();
                                prop.Description = propObj["description"]?.ToString();
                                
                                // Handle reference
                                var reference = propObj["$ref"]?.ToString();
                                if (!string.IsNullOrEmpty(reference))
                                {
                                    prop.Reference = NormalizeReference(reference);
                                }
                                
                                // Handle array
                                if (prop.Type == "array")
                                {
                                    var items = propObj["items"] as JObject;
                                    if (items != null)
                                    {
                                        prop.Items = new SwaggerProperty();
                                        prop.Items.Type = items["type"]?.ToString();
                                        
                                        var itemsRef = items["$ref"]?.ToString();
                                        if (!string.IsNullOrEmpty(itemsRef))
                                        {
                                            prop.Items.Reference = NormalizeReference(itemsRef);
                                        }
                                    }
                                }
                                
                                definition.Properties.Add(property.Name, prop);
                            }
                        }
                    }
                    
                    // Parse required fields
                    var required = definitionObj["required"] as JArray;
                    if (required != null)
                    {
                        definition.Required = required.ToObject<List<string>>();
                    }
                }

                result.Add(name, definition);
            }

            return result;
        }

        private string NormalizeReference(string reference)
        {
            // Convert "#/definitions/Model" to "Model" (Swagger 2.0)
            if (reference.StartsWith("#/definitions/"))
            {
                return reference.Substring("#/definitions/".Length);
            }
            // Convert "#/components/schemas/Model" to "Model" (OpenAPI 3.0)
            else if (reference.StartsWith("#/components/schemas/"))
            {
                return reference.Substring("#/components/schemas/".Length);
            }
            return reference;
        }
    }
} 