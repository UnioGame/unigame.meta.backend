using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Modules.unity.meta.service.Modules.WebProvider
{
    /// <summary>
    /// Generates C# contract files from Swagger operations
    /// </summary>
    public class ContractTemplateGenerator
    {
        // Dictionary for schema name to DTO class name mapping
        private Dictionary<string, string> _schemaToClassNameMap;
        
        // Namespace for generated DTO classes
        private readonly string _namespace;
        
        public ContractTemplateGenerator(Dictionary<string, string> schemaMap = null)
        {
            _schemaToClassNameMap = schemaMap ?? new Dictionary<string, string>();
            _namespace = "Game.Modules.WebProvider.Contracts";
        }
        
        public ContractTemplateGenerator(Dictionary<string, string> schemaMap, string dtoNamespace)
        {
            _schemaToClassNameMap = schemaMap ?? new Dictionary<string, string>();
            _namespace = string.IsNullOrEmpty(dtoNamespace) ? "Game.Modules.WebProvider.Contracts" : dtoNamespace;
        }
        
        // Method to get class name from schema name
        public string GetClassNameForSchema(string schemaName)
        {
            if (_schemaToClassNameMap.TryGetValue(schemaName, out var className))
            {
                // If name contains namespace separator, return as is
                if (className.Contains("."))
                {
                    return className;
                }
                
                // Otherwise add DTO namespace
                return $"{_namespace}.Dto.{className}";
            }
            
            // If no mapping exists, use PascalCase schema name
            return $"{_namespace}.Dto.{OpenApiHelpers.ToPascalCase(schemaName)}";
        }

        /// <summary>
        /// Generates a contract class for a specific endpoint
        /// </summary>
        public string GenerateContract(string path, string method, SwaggerOperation operation, string requestUrl, string inputType, string outputType, string errorType = null)
        {
            HashSet<string> additionalNamespaces = new HashSet<string>();
            string contractClassName = OpenApiHelpers.CleanOperationName(operation.OperationId);
            
            // Определяем базовый тип контракта (с errorType или без)
            string baseType;
            if (!string.IsNullOrEmpty(errorType))
            {
                baseType = $"RestContract<{inputType}, {outputType}, {errorType}>";
                AddNamespaceImport(errorType, additionalNamespaces);
            }
            else
            {
                baseType = $"RestContract<{inputType}, {outputType}>";
            }
            
            AddNamespaceImport(inputType, additionalNamespaces);
            AddNamespaceImport(outputType, additionalNamespaces);
            
            // Формируем переопределения свойств вместо конструктора
            string constructorText = $@"        /// <summary>
        /// The API path for this request
        /// </summary>
        public override string Path => ""{requestUrl}"";

        /// <summary>
        /// The type of request
        /// </summary>
        public override WebRequestType RequestType => WebRequestType.{OpenApiHelpers.GetWebRequestType(method)};";
            
            // Формируем дополнительные импорты пространств имен
            string additionalNamespacesText = string.Join(Environment.NewLine, additionalNamespaces.Select(n => $"using {n};"));
            if (!string.IsNullOrEmpty(additionalNamespacesText))
            {
                additionalNamespacesText = additionalNamespacesText + Environment.NewLine;
            }
            
            // Формируем описание из сводки операции
            string descriptionText = string.Empty;
            if (!string.IsNullOrEmpty(operation.Summary))
            {
                descriptionText = $@"    /// <summary>
    /// {operation.Summary}
    /// </summary>";
            }
            
            // Заменяем плейсхолдеры в шаблоне
            string contractTemplate = LoadContractTemplate();
            string contractCode = contractTemplate
                .Replace("$className$", contractClassName)
                .Replace("$baseType$", baseType)
                .Replace("$namespace$", _namespace)
                .Replace("$description$", OpenApiHelpers.FormatXmlDocumentation(operation.Summary))
                .Replace("$constructor$", constructorText)
                .Replace("{{ADDITIONAL_NAMESPACES}}", additionalNamespacesText);
            
            return contractCode;
        }

        private void AddNamespaceImport(string typeName, HashSet<string> namespaces)
        {
            if (string.IsNullOrEmpty(typeName) || typeName == "void" || typeName == "object")
                return;
            
            // Обрабатываем параметризованные типы (например ResponseDataDTO<T>)
            if (typeName.Contains("<"))
            {
                string baseType = typeName.Substring(0, typeName.IndexOf("<"));
                string innerType = typeName.Substring(typeName.IndexOf("<") + 1, typeName.LastIndexOf(">") - typeName.IndexOf("<") - 1);
                
                // Рекурсивно обрабатываем внутренний тип
                AddNamespaceImport(innerType, namespaces);
                
                // Определяем пространство имен базового типа
                if (baseType.Contains("."))
                {
                    string baseNamespace = baseType.Substring(0, baseType.LastIndexOf("."));
                    if (!baseNamespace.StartsWith("System"))
                    {
                        namespaces.Add(baseNamespace);
                    }
                }
                
                return;
            }
            
            // Обрабатываем обычные типы
            if (typeName.Contains("."))
            {
                string namespaceStr = typeName.Substring(0, typeName.LastIndexOf("."));
                if (!namespaceStr.StartsWith("System"))
                {
                    namespaces.Add(namespaceStr);
                }
            }
        }

        /// <summary>
        /// Generate a DTO class from a Swagger definition
        /// </summary>
        public string GenerateDto(string className, SwaggerDefinition definition)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Newtonsoft.Json;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {_namespace}.Dto");
            sb.AppendLine("{");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            if (definition.Properties != null)
            {
                foreach (var property in definition.Properties)
                {
                    sb.Append(GeneratePropertyCode(property.Key, property.Value));
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
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
                    pathParts[i] = "By" + OpenApiHelpers.ToPascalCase(pathParts[i].Trim('{', '}'));
                }
            }
            
            // Create a name from the path parts
            string namePart = string.Join("", pathParts.Select(OpenApiHelpers.ToPascalCase));
            
            // Add method to the name
            return $"{OpenApiHelpers.ToPascalCase(method)}{namePart}Contract";
        }

        /// <summary>
        /// Determines the input type for a contract based on the operation parameters
        /// </summary>
        public string GetInputType(SwaggerOperation operation)
        {
            // Checking for requestBody (OpenAPI 3.0)
            if (operation.RequestBody?.Schema != null)
            {
                // Если у тела запроса есть прямая ссылка на схему
                if (!string.IsNullOrEmpty(operation.RequestBody.Schema.Reference))
                {
                    // Используем маппинг для получения имени класса с учетом title
                    return GetClassNameForSchema(operation.RequestBody.Schema.Reference);
                }
                else if (operation.RequestBody.Schema.Type == "array")
                {
                    // Handle array types
                    if (operation.RequestBody.Schema.Items != null)
                    {
                        string itemType = GetPropertyTypeName(operation.RequestBody.Schema.Items);
                        return $"List<{itemType}>";
                    }
                    return "List<object>";
                }
            }
            
            // For POST, PUT, PATCH methods, typically use the body parameter as input (Swagger 2.0)
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
                        // For objects without direct reference, use DTO based on operationId
                        string operationId = operation.OperationId ?? $"{operation.Summary}";
                        return $"{OpenApiHelpers.CleanOperationName(operationId)}Input";
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
                return $"{OpenApiHelpers.CleanOperationName(operationId)}Input";
            }
            
            return "object";
        }

        /// <summary>
        /// Determines the output type for a contract based on the operation responses
        /// </summary>
        public string GetOutputType(SwaggerOperation operation)
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
                    // For object types without direct reference, create custom DTO
                    string operationId = operation.OperationId ?? $"{operation.Summary}";
                    return $"{OpenApiHelpers.CleanOperationName(operationId)}Output";
                }
                else
                {
                    // For primitive types
                    return OpenApiHelpers.MapTypeToCSharp(successResponse.Schema.Type, successResponse.Schema.Format);
                }
            }
            
            return "object";
        }

        /// <summary>
        /// Определяет тип ошибки для контракта на основе ответов с ошибками в операции
        /// </summary>
        public string GetErrorType(SwaggerOperation operation)
        {
            // Ищем ответы с ошибками (4XX или 5XX)
            var errorResponses = operation.Responses.Where(r => 
                r.Key.StartsWith("4") || 
                r.Key.StartsWith("5") || 
                r.Key.Contains("4XX") || 
                r.Key.Contains("5XX") || 
                r.Key.Contains("default")).ToList();
            
            foreach (var errorResponse in errorResponses)
            {
                if (errorResponse.Value?.Schema != null)
                {
                    // Если у ответа с ошибкой есть прямая ссылка на схему
                    if (!string.IsNullOrEmpty(errorResponse.Value.Schema.Reference))
                    {
                        // Используем маппинг для получения имени класса с учетом title
                        return GetClassNameForSchema(errorResponse.Value.Schema.Reference);
                    }
                    else if (errorResponse.Value.Schema.Type == "object" && errorResponse.Value.Schema.Properties != null)
                    {
                        // For object types without direct reference, create custom DTO for error
                        string operationId = operation.OperationId ?? $"{operation.Summary}";
                        return $"{OpenApiHelpers.CleanOperationName(operationId)}ErrorOutput";
                    }
                }
            }
            
            // Если не нашли специфичный тип ошибки, используем общий тип ErrorResponseDTO
            return $"{_namespace}.Dto.ErrorResponseDTO";
        }

        private string GetPropertyTypeName(SwaggerSchema schema)
        {
            if (!string.IsNullOrEmpty(schema.Reference))
            {
                return GetClassNameFromReference(schema.Reference);
            }
            
            if (schema.Type == "array" && schema.Items != null)
            {
                if (!string.IsNullOrEmpty(schema.Items.Reference))
                {
                    string itemTypeName = GetClassNameFromReference(schema.Items.Reference);
                    return $"List<{itemTypeName}>";
                }
                else
                {
                    string itemTypeName = OpenApiHelpers.MapTypeToCSharp(schema.Items.Type, schema.Items.Format);
                    return $"List<{itemTypeName}>";
                }
            }
            
            return OpenApiHelpers.MapTypeToCSharp(schema.Type, schema.Format);
        }

        private string GetPropertyTypeName(SwaggerProperty property)
        {
            // Direct reference to another schema
            if (!string.IsNullOrEmpty(property.Reference))
            {
                // Special handling for ErrorCode
                if (property.Reference.EndsWith("ErrorCode"))
                {
                    return "ErrorCode";
                }
                
                return GetClassNameFromReference(property.Reference);
            }
            
            // Array with items that reference another schema
            if (property.Type == "array" && property.Items != null)
            {
                if (!string.IsNullOrEmpty(property.Items.Reference))
                {
                    // Special handling for ErrorCode array
                    if (property.Items.Reference.EndsWith("ErrorCode"))
                    {
                        return "List<ErrorCode>";
                    }
                    
                    string itemTypeName = GetClassNameFromReference(property.Items.Reference);
                    return $"List<{itemTypeName}>";
                }
                else
                {
                    string itemTypeName = OpenApiHelpers.MapTypeToCSharp(property.Items.Type, property.Items.Format);
                    return $"List<{itemTypeName}>";
                }
            }
            
            // Regular type
            return OpenApiHelpers.MapTypeToCSharp(property.Type, property.Format);
        }
        
        /// <summary>
        /// Gets class name from schema reference using mapping dictionary
        /// </summary>
        private string GetClassNameFromReference(string reference)
        {
            string schemaName = OpenApiHelpers.NormalizeReference(reference);
            
            // Check if mapping exists for this schema
            if (_schemaToClassNameMap.TryGetValue(schemaName, out var className))
            {
                // If name contains namespace separator, return as is
                if (className.Contains("."))
                {
                    return className;
                }
                
                // Otherwise add DTO namespace
                return $"{_namespace}.Dto.{className}";
            }
            
            // If no mapping exists, use PascalCase schema name
            return $"{_namespace}.Dto.{OpenApiHelpers.ToPascalCase(schemaName)}";
        }

        private string GenerateProperty(string propertyName, SwaggerProperty property, Dictionary<string, string> schemaToClassName)
        {
            string typeName = GetPropertyTypeName(property);
            string pascalCaseName = OpenApiHelpers.ToPascalCase(propertyName);
            
            var sb = new StringBuilder();
            
            // Добавляем атрибут JsonProperty, если оригинальное имя отличается от Pascal Case
            if (!string.IsNullOrEmpty(property.OriginalName) && property.OriginalName != pascalCaseName)
            {
                sb.AppendLine($"        [JsonProperty(\"{property.OriginalName}\")]");
            }
            
            if (!string.IsNullOrEmpty(property.Description))
            {
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// {property.Description}");
                sb.AppendLine($"        /// </summary>");
            }
            
            sb.AppendLine($"        public {typeName} {pascalCaseName} {{ get; set; }}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Generates a ResponseDataDTO wrapper class to handle server responses with data containers
        /// </summary>
        public string GenerateResponseDataWrapper(string responseDataField)
        {
            StringBuilder sb = new StringBuilder();
            
            // Add usings
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Newtonsoft.Json;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            
            // Add namespace
            sb.AppendLine($"namespace {_namespace}.Dto");
            sb.AppendLine("{");
            
            // Add class documentation
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Generic response wrapper for API responses that wrap data in a container");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine("    public class ResponseDataDTO<T>");
            sb.AppendLine("    {");
            
            // Add data property
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// The actual response data from the API");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        [JsonProperty(\"{responseDataField}\")]");
            sb.AppendLine($"        [field: SerializeField]");
            sb.AppendLine($"        public T {OpenApiHelpers.ToPascalCase(responseDataField)} {{ get; set; }}");
            sb.AppendLine();
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Gets a wrapped type name for response data containers
        /// </summary>
        public string GetResponseContainerType(string originalTypeName)
        {
            string result = "ResponseDataDTO<" + originalTypeName + ">";
            Debug.Log($"[DEBUG] GetResponseContainerType: originalTypeName='{originalTypeName}', result='{result}'");
            return result;
        }

        private string GeneratePropertyCode(string propertyName, SwaggerProperty property)
        {
            var sb = new StringBuilder();
            
            // Получаем тип свойства, учитывая ссылки на другие схемы
            string propertyType = GetPropertyTypeName(property);
            
            // Конвертируем имя свойства в PascalCase для C#
            string csharpPropertyName = OpenApiHelpers.ToPascalCase(propertyName);
            
            // Добавляем JsonProperty атрибут, если оригинальное имя отличается от C# имени
            if (!string.IsNullOrEmpty(property.OriginalName) && property.OriginalName != csharpPropertyName)
            {
                sb.AppendLine($"        [JsonProperty(\"{property.OriginalName}\")]");
            }
            
            // Добавляем описание, если оно есть
            if (!string.IsNullOrEmpty(property.Description))
            {
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// {property.Description}");
                sb.AppendLine($"        /// </summary>");
            }
            
            // Добавляем атрибут SerializeField для отображения в Unity Inspector
            sb.AppendLine($"        [field: SerializeField]");
            
            // Генерируем само свойство
            sb.AppendLine($"        public {propertyType} {csharpPropertyName} {{ get; set; }}");
            
            return sb.ToString();
        }

        private string LoadContractTemplate()
        {
            return @"using System;
using System.Collections.Generic;
using System.Text;
using UniGame.MetaBackend;
using $namespace$.Dto;
using Game.Modules.WebProvider.Contracts;
using UniGame.MetaBackend.Runtime.WebService;
using Newtonsoft.Json;

namespace $namespace$
{
    /// <summary>
    /// $description$
    /// </summary>
    [Serializable]
    public class $className$ : $baseType$
    {
        $constructor$
    }
}";
        }
    }
} 