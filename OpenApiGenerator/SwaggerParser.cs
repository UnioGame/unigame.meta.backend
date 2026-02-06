using System;
using System.Collections.Generic;
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
                
                // Parse basePath (Swagger 2.0)
                definition.BasePath = jsonObject["basePath"]?.ToString();
                
                // Parse servers array (OpenAPI 3.0)
                var servers = jsonObject["servers"] as JArray;
                if (servers != null)
                {
                    definition.Servers = ParseServers(servers);
                    // If basePath is not set, try to extract from first server URL
                    if (string.IsNullOrEmpty(definition.BasePath) && definition.Servers.Count > 0)
                    {
                        var firstServerUrl = definition.Servers[0].Url;
                        if (!string.IsNullOrEmpty(firstServerUrl))
                        {
                            var uri = new Uri(firstServerUrl, UriKind.RelativeOrAbsolute);
                            if (uri.IsAbsoluteUri)
                            {
                                definition.BasePath = uri.PathAndQuery;
                            }
                            else
                            {
                                definition.BasePath = firstServerUrl;
                            }
                        }
                    }
                }

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

        private List<SwaggerServer> ParseServers(JArray servers)
        {
            var result = new List<SwaggerServer>();
            
            foreach (var serverToken in servers)
            {
                var serverObj = serverToken as JObject;
                if (serverObj != null)
                {
                    var server = new SwaggerServer
                    {
                        Url = serverObj["url"]?.ToString(),
                        Description = serverObj["description"]?.ToString()
                    };
                    result.Add(server);
                }
            }
            
            return result;
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

                    // Parse requestBody (OpenAPI 3.0)
                    var requestBody = operationObj["requestBody"] as JObject;
                    if (requestBody != null)
                    {
                        operation.RequestBody = ParseRequestBody(requestBody);
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

        /// <summary>
        /// Parses requestBody from OpenAPI 3.0
        /// Handles multiple content types, preferring application/json
        /// </summary>
        private SwaggerRequestBody ParseRequestBody(JObject requestBody)
        {
            var result = new SwaggerRequestBody
            {
                Description = requestBody["description"]?.ToString(),
                Required = (bool?)requestBody["required"] ?? false
            };

            // Get content types
            var content = requestBody["content"] as JObject;
            if (content != null)
            {
                // Get all available content types
                var contentTypes = new List<string>();
                foreach (var prop in content.Properties())
                {
                    contentTypes.Add(prop.Name);
                }
                
                // Get preferred content type (prefers application/json)
                var preferredContentType = OpenApiHelpers.GetPreferredContentType(contentTypes);
                
                // Try to parse schema from preferred content type
                var contentObj = content[preferredContentType] as JObject;
                if (contentObj != null)
                {
                    var schema = contentObj["schema"] as JObject;
                    if (schema != null)
                    {
                        result.Schema = ParseSchema(schema);
                    }
                }
                else
                {
                    // Fallback: try application/json directly
                    var jsonContent = content[OpenApiHelpers.ContentTypes.ApplicationJson] as JObject;
                    if (jsonContent != null)
                    {
                        var schema = jsonContent["schema"] as JObject;
                        if (schema != null)
                        {
                            result.Schema = ParseSchema(schema);
                        }
                    }
                }
            }

            return result;
        }

        private List<SwaggerParameter> ParseParameters(JArray parameters)
        {
            var result = new List<SwaggerParameter>();

            foreach (var parameterToken in parameters)
            {
                var parameter = new SwaggerParameter();
                parameter.Name = (string)parameterToken["name"];
                parameter.In = (string)parameterToken["in"];
                parameter.Description = (string)parameterToken["description"];
                parameter.Required = (bool?)parameterToken["required"] ?? false;
                
                var schema = parameterToken["schema"];
                if (schema != null)
                {
                    parameter.Schema = ParseSchema((JObject)schema);
                }
                else
                {
                    parameter.Type = (string)parameterToken["type"];
                    parameter.Format = (string)parameterToken["format"];
                }
                
                // Сохраняем оригинальное имя параметра
                parameter.OriginalName = parameter.Name;
                
                result.Add(parameter);
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
                    
                    // Try to get schema directly (Swagger 2.0)
                    var schema = responseObj["schema"] as JObject;
                    if (schema != null)
                    {
                        response.Schema = ParseSchema(schema);
                    }
                    else
                    {
                        // Try to get schema from content (OpenAPI 3.0)
                        var content = responseObj["content"] as JObject;
                        if (content != null)
                        {
                            // Get all available content types
                            var contentTypes = new List<string>();
                            foreach (var prop in content.Properties())
                            {
                                contentTypes.Add(prop.Name);
                            }
                            
                            // Get preferred content type (prefers application/json)
                            var preferredContentType = OpenApiHelpers.GetPreferredContentType(contentTypes);
                            
                            // Parse schema from preferred content type
                            var contentObj = content[preferredContentType] as JObject;
                            if (contentObj != null)
                            {
                                var contentSchema = contentObj["schema"] as JObject;
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
                result.Reference = OpenApiHelpers.NormalizeReference(reference);
                return result;
            }

            // Handle allOf (schema composition/inheritance)
            var allOf = schema["allOf"] as JArray;
            if (allOf != null && allOf.Count > 0)
            {
                result.AllOf = new List<SwaggerSchema>();
                foreach (var item in allOf)
                {
                    var itemObj = item as JObject;
                    if (itemObj != null)
                    {
                        result.AllOf.Add(ParseSchema(itemObj));
                    }
                }
                // Set type to object for composition
                result.Type = "object";
                return result;
            }

            // Handle anyOf (union types)
            var anyOf = schema["anyOf"] as JArray;
            if (anyOf != null && anyOf.Count > 0)
            {
                result.AnyOf = new List<SwaggerSchema>();
                foreach (var item in anyOf)
                {
                    var itemObj = item as JObject;
                    if (itemObj != null)
                    {
                        result.AnyOf.Add(ParseSchema(itemObj));
                    }
                }
                // Default to object for union types
                result.Type = "object";
                return result;
            }

            // Handle oneOf (discriminated union)
            var oneOf = schema["oneOf"] as JArray;
            if (oneOf != null && oneOf.Count > 0)
            {
                result.OneOf = new List<SwaggerSchema>();
                foreach (var item in oneOf)
                {
                    var itemObj = item as JObject;
                    if (itemObj != null)
                    {
                        result.OneOf.Add(ParseSchema(itemObj));
                    }
                }
                
                // Parse discriminator if present
                var discriminator = schema["discriminator"] as JObject;
                if (discriminator != null)
                {
                    result.Discriminator = new SwaggerDiscriminator
                    {
                        PropertyName = discriminator["propertyName"]?.ToString(),
                        Mapping = new Dictionary<string, string>()
                    };
                    
                    var mapping = discriminator["mapping"] as JObject;
                    if (mapping != null)
                    {
                        foreach (var prop in mapping.Properties())
                        {
                            result.Discriminator.Mapping[prop.Name] = prop.Value.ToString();
                        }
                    }
                }
                
                // Default to object for discriminated union
                result.Type = "object";
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
                            var propSchema = ParseSchema(propObj);
                            propSchema.OriginalName = property.Name;
                            result.Properties.Add(property.Name, propSchema);
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

            if (definitions == null)
            {
                return result;
            }

            foreach (var definitionPair in definitions)
            {
                var definitionName = definitionPair.Key;
                var definitionObject = (JObject)definitionPair.Value;

                var definition = new SwaggerDefinition
                {
                    Name = definitionName
                };

                if (definitionObject["title"] != null)
                {
                    definition.Title = (string)definitionObject["title"];
                }

                var properties = definitionObject["properties"];
                if (properties != null)
                {
                    definition.Properties = new Dictionary<string, SwaggerProperty>();

                    foreach (var propertyPair in (JObject)properties)
                    {
                        var propertyName = propertyPair.Key;
                        var propertyObject = (JObject)propertyPair.Value;

                        var property = ParsePropertyObject(propertyObject);
                        // Сохраняем оригинальное имя свойства
                        property.OriginalName = propertyName;
                        
                        definition.Properties.Add(propertyName, property);
                    }
                }

                result.Add(definitionName, definition);
            }

            return result;
        }

        /// <summary>
        /// Normalizes references using shared helper
        /// </summary>
        private string NormalizeReference(string reference)
        {
            return OpenApiHelpers.NormalizeReference(reference);
        }

        private SwaggerProperty ParsePropertyObject(JObject propertyObject)
        {
            var property = new SwaggerProperty();
            
            property.Type = (string)propertyObject["type"];
            property.Format = (string)propertyObject["format"];
            property.Description = (string)propertyObject["description"];
            property.Nullable = (bool?)propertyObject["nullable"] ?? false;
            property.Deprecated = (bool?)propertyObject["deprecated"] ?? false;
            
            // Parse enum values
            var enumValues = propertyObject["enum"] as JArray;
            if (enumValues != null && enumValues.Count > 0)
            {
                property.Enum = new List<object>();
                foreach (var value in enumValues)
                {
                    property.Enum.Add(value.ToObject<object>());
                }
            }
            
            // Handle $ref
            var reference = (string)propertyObject["$ref"];
            if (!string.IsNullOrEmpty(reference))
            {
                property.Reference = NormalizeReference(reference);
                
                // Special handling for ErrorCode enum
                if (property.Reference.EndsWith("ErrorCode"))
                {
                    property.Type = "integer";
                    property.Format = "int32";
                    property.Reference = null;
                }
            }
            
            // Handle allOf composition
            var allOf = propertyObject["allOf"] as JArray;
            if (allOf != null && allOf.Count > 0)
            {
                property.AllOf = new List<SwaggerProperty>();
                foreach (var item in allOf)
                {
                    var itemObj = item as JObject;
                    if (itemObj != null)
                    {
                        property.AllOf.Add(ParsePropertyObject(itemObj));
                    }
                }
                property.Type = "object";
            }
            
            // Handle anyOf composition
            var anyOf = propertyObject["anyOf"] as JArray;
            if (anyOf != null && anyOf.Count > 0)
            {
                property.AnyOf = new List<SwaggerProperty>();
                foreach (var item in anyOf)
                {
                    var itemObj = item as JObject;
                    if (itemObj != null)
                    {
                        property.AnyOf.Add(ParsePropertyObject(itemObj));
                    }
                }
                property.Type = "object";
            }
            
            // Handle oneOf composition
            var oneOf = propertyObject["oneOf"] as JArray;
            if (oneOf != null && oneOf.Count > 0)
            {
                property.OneOf = new List<SwaggerProperty>();
                foreach (var item in oneOf)
                {
                    var itemObj = item as JObject;
                    if (itemObj != null)
                    {
                        property.OneOf.Add(ParsePropertyObject(itemObj));
                    }
                }
                
                // Default to object, but check for specific patterns
                property.Type = "object";
                
                // Special case: data field in error responses
                if (propertyObject.Path.EndsWith(".data"))
                {
                    property.Type = "string";
                }
            }
            
            // Handle array types
            if (property.Type == "array")
            {
                var items = propertyObject["items"] as JObject;
                if (items != null)
                {
                    property.Items = ParsePropertyObject(items);
                }
            }
            
            // Handle object types with inline properties
            if (property.Type == "object")
            {
                var properties = propertyObject["properties"] as JObject;
                if (properties != null)
                {
                    property.Properties = new Dictionary<string, SwaggerProperty>();
                    foreach (var prop in properties.Properties())
                    {
                        var propObj = prop.Value as JObject;
                        if (propObj != null)
                        {
                            var parsedProp = ParsePropertyObject(propObj);
                            parsedProp.OriginalName = prop.Name;
                            property.Properties[prop.Name] = parsedProp;
                        }
                    }
                }
            }
            
            return property;
        }
    }
} 