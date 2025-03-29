using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Game.Runtime.Services.WebService;

namespace Game.Modules.unity.meta.service.Modules.WebProvider
{
    /// <summary>
    /// Generates C# contract files from Swagger operations
    /// </summary>
    public class ContractTemplateGenerator
    {
        private readonly Dictionary<string, string> _typeMapping = new Dictionary<string, string>
        {
            { "integer", "int" },
            { "number", "float" },
            { "boolean", "bool" },
            { "string", "string" },
            { "array", "List<{0}>" },
            { "object", "Dictionary<string, object>" }
        };

        private readonly Dictionary<string, string> _formatMapping = new Dictionary<string, string>
        {
            { "int32", "int" },
            { "int64", "long" },
            { "float", "float" },
            { "double", "double" },
            { "byte", "byte" },
            { "binary", "byte[]" },
            { "date", "DateTime" },
            { "date-time", "DateTime" },
            { "password", "string" },
            { "uuid", "Guid" }
        };

        // Словарь для преобразования имен схем в имена классов DTO (с учетом title)
        private Dictionary<string, string> _schemaToClassNameMap;
        
        public ContractTemplateGenerator(Dictionary<string, string> schemaMap = null)
        {
            _schemaToClassNameMap = schemaMap ?? new Dictionary<string, string>();
        }
        
        // Метод для получения имени класса по имени схемы
        public string GetClassNameForSchema(string schemaName)
        {
            return _schemaToClassNameMap.TryGetValue(schemaName, out var className) 
                ? className 
                : schemaName;
        }

        /// <summary>
        /// Generates a contract class for a specific endpoint
        /// </summary>
        public string GenerateContract(string path, string method, SwaggerOperation operation, string apiTemplate)
        {
            try
            {
                string contractName = GetContractName(path, method);
                StringBuilder sb = new StringBuilder();

                // Add usings
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Text;");
                sb.AppendLine("using UniGame.MetaBackend;");
                sb.AppendLine("using Game.Modules.WebProvider.Contracts.DTO;");
                sb.AppendLine("using Game.Runtime.Services.WebService;");
                sb.AppendLine();
                
                // Add namespace
                sb.AppendLine("namespace Game.Modules.WebProvider.Contracts");
                sb.AppendLine("{");
                
                // Add summary comments
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {operation.Summary ?? $"Contract for {method.ToUpper()} {path}"}");
                
                if (!string.IsNullOrEmpty(operation.Description))
                {
                    sb.AppendLine("    /// <para>");
                    sb.AppendLine($"    /// {operation.Description}");
                    sb.AppendLine("    /// </para>");
                }
                
                sb.AppendLine("    /// </summary>");
                
                // Add class declaration
                sb.AppendLine("    [Serializable]");
                
                // Determine input and output types
                string inputType = GetInputType(operation);
                string outputType = GetOutputType(operation);
                
                // Add class with proper generic arguments
                if (!string.IsNullOrEmpty(inputType) && !string.IsNullOrEmpty(outputType))
                {
                    sb.AppendLine($"    public class {contractName} : RestContract<{inputType}, {outputType}>");
                }
                else if (!string.IsNullOrEmpty(inputType))
                {
                    sb.AppendLine($"    public class {contractName} : RestContract<{inputType}, object>");
                }
                else if (!string.IsNullOrEmpty(outputType))
                {
                    sb.AppendLine($"    public class {contractName} : RestContract<object, {outputType}>");
                }
                else
                {
                    sb.AppendLine($"    public class {contractName} : RestContract<object, object>");
                }
                
                sb.AppendLine("    {");
                
                // Process URL path
                string urlPath = path;
                
                // Get all path parameters
                var pathParams = operation.Parameters.Where(p => p.In == "path").ToList();
                
                // Add Path property
                string formattedPath = string.Format(apiTemplate, urlPath.TrimStart('/'));
                
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// The API path for this request");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public override string Path => \"{formattedPath}\";");
                sb.AppendLine();
                
                // Add RequestType property based on HTTP method
                string requestType = GetRequestTypeFromMethod(method);
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// The type of request");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public override WebRequestType RequestType => WebRequestType.{requestType};");
                sb.AppendLine();
                
                // Only add path and query parameters as fields in the contract if we don't have a specific Input DTO
                bool hasInputDto = !string.IsNullOrEmpty(inputType) && inputType != "object";
                
                if (!hasInputDto)
                {
                    // Add path parameters as fields if they're not part of the DTO
                    foreach (var param in pathParams)
                    {
                        string typeName = MapSwaggerTypeToCs(param.Type, param.Format);
                        sb.AppendLine($"        /// <summary>");
                        sb.AppendLine($"        /// {param.Description ?? $"Path parameter: {param.Name}"}");
                        sb.AppendLine($"        /// </summary>");
                        sb.AppendLine($"        public {typeName} {ToPascalCase(param.Name)} {{ get; set; }}");
                        sb.AppendLine();
                    }
                    
                    // Add query parameters as fields if they're not part of the DTO
                    var queryParams = operation.Parameters.Where(p => p.In == "query").ToList();
                    if (queryParams.Any())
                    {
                        sb.AppendLine("        // Query parameters");
                        foreach (var param in queryParams)
                        {
                            string typeName = MapSwaggerTypeToCs(param.Type, param.Format);
                            sb.AppendLine($"        /// <summary>");
                            sb.AppendLine($"        /// {param.Description ?? $"Query parameter: {param.Name}"}");
                            sb.AppendLine($"        /// </summary>");
                            sb.AppendLine($"        public {typeName} {ToPascalCase(param.Name)} {{ get; set; }}");
                            sb.AppendLine();
                        }
                    }
                }
                
                sb.AppendLine("    }");
                sb.AppendLine("}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating contract for {method} {path}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate a DTO class from a Swagger definition
        /// </summary>
        public string GenerateDto(string name, SwaggerDefinition definition)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                // Add usings
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine();
                
                // Add namespace
                sb.AppendLine("namespace Game.Modules.WebProvider.Contracts.DTO");
                sb.AppendLine("{");
                
                // Add class declaration
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// DTO for {name}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    [Serializable]");
                sb.AppendLine($"    public class {name}");
                sb.AppendLine("    {");
                
                // Add properties
                if (definition.Properties != null)
                {
                    foreach (var property in definition.Properties)
                    {
                        string propName = ToPascalCase(property.Key);
                        string propType = GetPropertyTypeName(property.Value);
                        bool isRequired = definition.Required?.Contains(property.Key) ?? false;
                        
                        // Add property documentation
                        sb.AppendLine($"        /// <summary>");
                        if (!string.IsNullOrEmpty(property.Value.Description))
                        {
                            sb.AppendLine($"        /// {property.Value.Description}");
                        }
                        else
                        {
                            sb.AppendLine($"        /// {property.Key} property");
                        }
                        if (isRequired)
                        {
                            sb.AppendLine($"        /// Required: true");
                        }
                        sb.AppendLine($"        /// </summary>");
                        
                        // Add property
                        sb.AppendLine($"        public {propType} {propName} {{ get; set; }}");
                        sb.AppendLine();
                    }
                }
                
                sb.AppendLine("    }");
                sb.AppendLine("}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating DTO for {name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the name of the contract class
        /// </summary>
        public string GetContractName(string path, string method)
        {
            // Extract meaningful parts from the path
            string[] pathParts = path.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToArray();
            
            // Remove path parameters (those in curly braces)
            for (int i = 0; i < pathParts.Length; i++)
            {
                if (pathParts[i].StartsWith("{") && pathParts[i].EndsWith("}"))
                {
                    pathParts[i] = "By" + ToPascalCase(pathParts[i].Trim('{', '}'));
                }
            }
            
            // Create a name from the path parts
            string namePart = string.Join("", pathParts.Select(ToPascalCase));
            
            // Add method to the name
            return $"{ToPascalCase(method)}{namePart}Contract";
        }

        private string GetInputType(SwaggerOperation operation)
        {
            // For POST, PUT, PATCH methods, typically use the body parameter as input
            if (operation.Parameters.Any(p => p.In == "body"))
            {
                var bodyParam = operation.Parameters.FirstOrDefault(p => p.In == "body");
                if (bodyParam?.Schema != null)
                {
                    if (!string.IsNullOrEmpty(bodyParam.Schema.Reference))
                    {
                        // Используем маппинг для получения имени класса с учетом title
                        return GetClassNameForSchema(bodyParam.Schema.Reference);
                    }
                    else if (bodyParam.Schema.Type == "array")
                    {
                        // Handle array types
                        if (bodyParam.Schema.Items != null)
                        {
                            string itemType = GetPropertyTypeName(bodyParam.Schema.Items);
                            return $"List<{itemType}>";
                        }
                        return "List<object>";
                    }
                    else if (bodyParam.Schema.Type == "object" && bodyParam.Schema.Properties != null)
                    {
                        // Для объектов без прямой ссылки, используем DTO на основе operationId
                        string operationId = operation.OperationId ?? $"{operation.Summary}";
                        return $"{CleanOperationName(operationId)}Input";
                    }
                }
            }
            
            // For GET, DELETE methods with query parameters, generate an input DTO
            // or use object if there's no parameters
            var queryParams = operation.Parameters.Where(p => p.In == "query" || p.In == "path").ToList();
            if (queryParams.Any())
            {
                // Use a custom input DTO for the parameters
                // The actual DTO should be created separately
                string operationId = operation.OperationId ?? $"{operation.Summary}";
                return $"{CleanOperationName(operationId)}Input";
            }
            
            return "object";
        }

        private string GetOutputType(SwaggerOperation operation)
        {
            var successResponse = operation.Responses.FirstOrDefault(r => r.Key == "200" || r.Key == "201").Value;
            if (successResponse?.Schema != null)
            {
                if (!string.IsNullOrEmpty(successResponse.Schema.Reference))
                {
                    // Используем маппинг для получения имени класса с учетом title
                    return GetClassNameForSchema(successResponse.Schema.Reference);
                }
                else if (successResponse.Schema.Type == "array")
                {
                    // Handle array types
                    if (successResponse.Schema.Items != null)
                    {
                        string itemType = GetPropertyTypeName(successResponse.Schema.Items);
                        return $"List<{itemType}>";
                    }
                    return "List<object>";
                }
                else if (successResponse.Schema.Type == "object" && successResponse.Schema.Properties != null)
                {
                    // Для объектных типов без прямой ссылки, создаем кастомный DTO
                    string operationId = operation.OperationId ?? $"{operation.Summary}";
                    return $"{CleanOperationName(operationId)}Output";
                }
                else
                {
                    // Use the mapped type for simple types
                    return MapSwaggerTypeToCs(successResponse.Schema.Type, successResponse.Schema.Format);
                }
            }
            
            return "object";
        }

        private string GetPropertyTypeName(SwaggerSchema schema)
        {
            if (!string.IsNullOrEmpty(schema.Reference))
            {
                // Используем маппинг для получения имени класса с учетом title
                return GetClassNameForSchema(schema.Reference);
            }
            
            if (schema.Type == "array")
            {
                if (schema.Items != null)
                {
                    string itemType = GetPropertyTypeName(schema.Items);
                    return string.Format(_typeMapping["array"], itemType);
                }
                return "List<object>";
            }
            
            return MapSwaggerTypeToCs(schema.Type, schema.Format);
        }

        private string GetPropertyTypeName(SwaggerProperty property)
        {
            if (!string.IsNullOrEmpty(property.Reference))
            {
                // Use the referenced type with mapping for title
                return GetClassNameForSchema(property.Reference);
            }
            
            if (property.Type == "array")
            {
                if (property.Items != null)
                {
                    string itemType = GetPropertyTypeName(property.Items);
                    return string.Format(_typeMapping["array"], itemType);
                }
                return "List<object>";
            }
            
            return MapSwaggerTypeToCs(property.Type, property.Format);
        }

        private string MapSwaggerTypeToCs(string type, string format)
        {
            if (string.IsNullOrEmpty(type))
                return "object";
                
            if (!string.IsNullOrEmpty(format) && _formatMapping.ContainsKey(format))
                return _formatMapping[format];
                
            if (_typeMapping.ContainsKey(type))
                return _typeMapping[type];
                
            return "object";
        }

        private string ToPascalCase(string text)
        {
            // Handle null or empty strings
            if (string.IsNullOrEmpty(text))
                return text;
                
            // Replace hyphens and underscores with spaces for splitting
            text = text.Replace('-', ' ').Replace('_', ' ');
            
            // Handle special characters
            var cleanText = Regex.Replace(text, "[^a-zA-Z0-9 ]", " ");
            
            // Split by spaces and make each part pascal case
            var parts = cleanText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    parts[i] = char.ToUpper(parts[i][0]) + 
                             (parts[i].Length > 1 ? parts[i].Substring(1).ToLower() : "");
                }
            }
            
            return string.Join("", parts);
        }

        /// <summary>
        /// Maps HTTP method to WebRequestType enum value
        /// </summary>
        private string GetRequestTypeFromMethod(string method)
        {
            switch (method.ToUpper())
            {
                case "GET":
                    return "Get";
                case "POST":
                    return "Post";
                case "PUT":
                    return "Put";
                case "DELETE":
                    return "Delete";
                case "PATCH":
                    return "Patch";
                case "HEAD":
                    return "Head";
                case "OPTIONS":
                    return "Options";
                default:
                    return "None"; // Default to None if method is unknown
            }
        }

        // Вспомогательный метод для очистки имен операций
        private string CleanOperationName(string operationId)
        {
            // Для имен с подчеркиванием и дефисами форматируем правильно в PascalCase
            return ToPascalCase(operationId);
        }
    }
} 