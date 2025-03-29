using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Modules.unity.meta.service.Modules.WebProvider
{
    /// <summary>
    /// Generates C# contract classes from Swagger JSON
    /// </summary>
    public class SwaggerContractGenerator
    {
        private readonly WebApiSettings _settings;
        private readonly SwaggerParser _parser;
        private readonly ContractTemplateGenerator _templateGenerator;
        
        // Словарь для маппинга внутренних имен схем на имена классов с учетом Title
        private readonly Dictionary<string, string> _schemaToClassNameMap = new Dictionary<string, string>();
        
        // Counters for statistics
        private int _newDtoFiles = 0;
        private int _overwrittenDtoFiles = 0;
        private int _newContractFiles = 0;
        private int _overwrittenContractFiles = 0;

        public SwaggerContractGenerator(WebApiSettings settings)
        {
            _settings = settings;
            _parser = new SwaggerParser();
            _templateGenerator = new ContractTemplateGenerator(_schemaToClassNameMap);
        }

        public void GenerateContracts()
        {
            try
            {
                // Reset counters and mappings
                _newDtoFiles = 0;
                _overwrittenDtoFiles = 0;
                _newContractFiles = 0;
                _overwrittenContractFiles = 0;
                _schemaToClassNameMap.Clear();
                
                // Validate settings
                if (string.IsNullOrEmpty(_settings.apiJsonPath))
                {
                    Debug.LogError("API JSON path is not specified in settings");
                    return;
                }

                if (!File.Exists(_settings.apiJsonPath))
                {
                    Debug.LogError($"API JSON file does not exist at path: {_settings.apiJsonPath}");
                    return;
                }

                if (string.IsNullOrEmpty(_settings.contractsOutFolder))
                {
                    Debug.LogError("Contracts output folder is not specified in settings");
                    return;
                }

                // Ensure output directories exist
                Directory.CreateDirectory(_settings.contractsOutFolder);
                Directory.CreateDirectory(_settings.dtoOutFolder);
                
                // Clean up output directories if specified in settings
                if (_settings.cleanUpOnGenerate)
                {
                    CleanupOutputDirectories();
                }

                // Read and parse Swagger JSON
                string jsonContent = File.ReadAllText(_settings.apiJsonPath);
                var apiDefinition = _parser.Parse(jsonContent);

                // Filter paths by allowed paths
                apiDefinition.Paths = FilterPathsByAllowedPaths(apiDefinition.Paths);

                // Собираем все используемые схемы данных из отфильтрованных путей
                var usedDefinitions = CollectUsedDefinitions(apiDefinition.Paths, apiDefinition.Definitions);

                // Подготавливаем словарь маппинга имен схем на имена классов
                foreach (var def in apiDefinition.Definitions)
                {
                    string className = !string.IsNullOrEmpty(def.Value.Title) ? def.Value.Title : def.Key;
                    _schemaToClassNameMap[def.Key] = className;
                }

                // Generate DTO classes only for definitions that are actually used
                var dtoFiles = GenerateDtoClasses(usedDefinitions);

                // Generate DTO classes for requests and responses
                var operationDtoFiles = GenerateOperationDtoClasses(apiDefinition.Paths);
                dtoFiles.AddRange(operationDtoFiles);

                // Generate contracts
                var contractFiles = GenerateContractClasses(apiDefinition.Paths);

                // Log generation statistics
                Debug.Log($"DTO files: {dtoFiles.Count} total ({_newDtoFiles} new, {_overwrittenDtoFiles} overwritten)");
                Debug.Log($"Contract files: {contractFiles.Count} total ({_newContractFiles} new, {_overwrittenContractFiles} overwritten)");
                
                #if UNITY_EDITOR
                // Reimport generated files
                if (dtoFiles.Count > 0 || contractFiles.Count > 0)
                {
                    Debug.Log("Reimporting generated files...");
                    try 
                    {
                        // Reimport contracts folder
                        string contractsPath = _settings.contractsOutFolder;
                        if (Path.IsPathRooted(contractsPath))
                        {
                            int assetsIndex = contractsPath.IndexOf("Assets");
                            if (assetsIndex >= 0)
                            {
                                contractsPath = contractsPath.Substring(assetsIndex);
                            }
                        }
                        
                        // Reimport DTO folder
                        string dtoPath = _settings.dtoOutFolder;
                        if (Path.IsPathRooted(dtoPath))
                        {
                            int assetsIndex = dtoPath.IndexOf("Assets");
                            if (assetsIndex >= 0)
                            {
                                dtoPath = dtoPath.Substring(assetsIndex);
                            }
                        }
                        
                        if (Directory.Exists(contractsPath))
                        {
                            AssetDatabase.ImportAsset(contractsPath, ImportAssetOptions.ImportRecursive);
                        }
                        
                        if (Directory.Exists(dtoPath))
                        {
                            AssetDatabase.ImportAsset(dtoPath, ImportAssetOptions.ImportRecursive);
                        }
                        
                        AssetDatabase.Refresh();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to reimport assets: {ex.Message}");
                    }
                }
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating contracts: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private List<string> GenerateDtoClasses(Dictionary<string, SwaggerDefinition> definitions)
        {
            var generatedFiles = new List<string>();

            foreach (var definition in definitions)
            {
                // Используем Title как имя класса DTO, если он указан, иначе используем имя схемы
                string dtoName = !string.IsNullOrEmpty(definition.Value.Title) 
                    ? definition.Value.Title 
                    : definition.Key;
                    
                // Запоминаем маппинг имени схемы на имя класса
                _schemaToClassNameMap[definition.Key] = dtoName;
                    
                string dtoCode = _templateGenerator.GenerateDto(dtoName, definition.Value);

                if (!string.IsNullOrEmpty(dtoCode))
                {
                    string fileName = dtoName + ".cs";
                    string filePath = Path.Combine(_settings.dtoOutFolder, fileName);
                    WriteFileWithTracking(filePath, dtoCode, isDto: true);
                    generatedFiles.Add(filePath);
                }
            }

            return generatedFiles;
        }

        private List<string> GenerateOperationDtoClasses(Dictionary<string, SwaggerPathItem> paths)
        {
            var generatedFiles = new List<string>();
            var processedOperations = new HashSet<string>();

            foreach (var path in paths)
            {
                foreach (var method in path.Value.Methods)
                {
                    var operation = method.Value;
                    string operationId = operation.OperationId ?? 
                        CleanOperationName(method.Key + "_" + string.Join("_", 
                            path.Key.Split('/').Where(p => !string.IsNullOrEmpty(p))));

                    // Generate Input DTO if needed
                    if (NeedsInputDto(operation) && !processedOperations.Contains(operationId + "Input"))
                    {
                        processedOperations.Add(operationId + "Input");
                        var inputDto = GenerateOperationInputDto(operationId, operation);
                        
                        if (!string.IsNullOrEmpty(inputDto))
                        {
                            string dtoClassName = $"{CleanOperationName(operationId)}Input";
                            string fileName = $"{dtoClassName}.cs";
                            string filePath = Path.Combine(_settings.dtoOutFolder, fileName);
                            WriteFileWithTracking(filePath, inputDto, isDto: true);
                            generatedFiles.Add(filePath);
                        }
                    }

                    // Generate Output DTO if needed
                    if (NeedsOutputDto(operation) && !processedOperations.Contains(operationId + "Output"))
                    {
                        processedOperations.Add(operationId + "Output");
                        var outputDto = GenerateOperationOutputDto(operationId, operation);
                        
                        if (!string.IsNullOrEmpty(outputDto))
                        {
                            string dtoClassName = $"{CleanOperationName(operationId)}Output";
                            string fileName = $"{dtoClassName}.cs";
                            string filePath = Path.Combine(_settings.dtoOutFolder, fileName);
                            WriteFileWithTracking(filePath, outputDto, isDto: true);
                            generatedFiles.Add(filePath);
                        }
                    }
                }
            }

            return generatedFiles;
        }

        private List<string> GenerateContractClasses(Dictionary<string, SwaggerPathItem> paths)
        {
            var generatedFiles = new List<string>();

            foreach (var path in paths)
            {
                foreach (var method in path.Value.Methods)
                {
                    string contractCode = _templateGenerator.GenerateContract(
                        path.Key,
                        method.Key,
                        method.Value,
                        _settings.apiTemplate
                    );

                    if (!string.IsNullOrEmpty(contractCode))
                    {
                        string fileName = _templateGenerator.GetContractName(path.Key, method.Key) + ".cs";
                        string filePath = Path.Combine(_settings.contractsOutFolder, fileName);
                        WriteFileWithTracking(filePath, contractCode, isDto: false);
                        generatedFiles.Add(filePath);
                    }
                }
            }

            return generatedFiles;
        }
        
        /// <summary>
        /// Writes a file to disk, tracking if it's a new file or overwriting an existing one
        /// </summary>
        private void WriteFileWithTracking(string filePath, string content, bool isDto)
        {
            bool fileExists = File.Exists(filePath);
            File.WriteAllText(filePath, content);
            
            if (isDto)
            {
                if (fileExists)
                {
                    _overwrittenDtoFiles++;
                }
                else
                {
                    _newDtoFiles++;
                }
            }
            else
            {
                if (fileExists)
                {
                    _overwrittenContractFiles++;
                }
                else
                {
                    _newContractFiles++;
                }
            }
        }

        private bool NeedsInputDto(SwaggerOperation operation)
        {
            // Проверяем, есть ли параметр body со ссылкой на схему
            var bodyParam = operation.Parameters.FirstOrDefault(p => p.In == "body");
            if (bodyParam?.Schema != null && !string.IsNullOrEmpty(bodyParam.Schema.Reference))
            {
                // Если у параметра тела запроса есть прямая ссылка на схему,
                // используем её напрямую и не создаем Input DTO
                return false;
            }

            // Check if we need to generate an Input DTO
            var pathParams = operation.Parameters.Where(p => p.In == "path").ToList();
            var queryParams = operation.Parameters.Where(p => p.In == "query").ToList();
            
            // If there are path or query parameters, we need an Input DTO
            return pathParams.Any() || queryParams.Any();
        }

        private bool NeedsOutputDto(SwaggerOperation operation)
        {
            // Check if we need to generate an Output DTO
            var successResponse = operation.Responses.FirstOrDefault(r => r.Key == "200" || r.Key == "201").Value;
            
            // Если у ответа есть прямая ссылка на схему, то не создаем Output DTO
            if (successResponse?.Schema != null && !string.IsNullOrEmpty(successResponse.Schema.Reference))
            {
                return false;
            }
            
            // If there's a success response with a schema of type object with properties, we need an Output DTO
            return successResponse?.Schema?.Type == "object" && 
                   successResponse.Schema.Properties != null && 
                   successResponse.Schema.Properties.Any();
        }

        private string GenerateOperationInputDto(string operationId, SwaggerOperation operation)
        {
            // Create a synthetic SwaggerDefinition from the path and query parameters
            var inputDefinition = new SwaggerDefinition
            {
                Type = "object",
                Properties = new Dictionary<string, SwaggerProperty>(),
                Required = new List<string>()
            };

            // Add path parameters
            foreach (var param in operation.Parameters.Where(p => p.In == "path" || p.In == "query"))
            {
                inputDefinition.Properties[param.Name] = new SwaggerProperty
                {
                    Type = param.Type,
                    Format = param.Format,
                    Description = param.Description
                };

                if (param.Required)
                {
                    inputDefinition.Required.Add(param.Name);
                }
            }

            // Add body parameter if it's not already a referenced type
            var bodyParam = operation.Parameters.FirstOrDefault(p => p.In == "body");
            if (bodyParam?.Schema != null && string.IsNullOrEmpty(bodyParam.Schema.Reference) && 
                bodyParam.Schema.Type == "object" && bodyParam.Schema.Properties != null)
            {
                foreach (var prop in bodyParam.Schema.Properties)
                {
                    inputDefinition.Properties[prop.Key] = new SwaggerProperty
                    {
                        Type = prop.Value.Type,
                        Format = prop.Value.Format,
                        Description = "From body: " + (prop.Value.Reference ?? "")
                    };
                }
            }

            // Generate DTO if there are any properties
            if (inputDefinition.Properties.Any())
            {
                return _templateGenerator.GenerateDto($"{CleanOperationName(operationId)}Input", inputDefinition);
            }

            return null;
        }

        private string GenerateOperationOutputDto(string operationId, SwaggerOperation operation)
        {
            var successResponse = operation.Responses.FirstOrDefault(r => r.Key == "200" || r.Key == "201").Value;
            
            // If there's a success response with a schema of type object, generate an Output DTO
            if (successResponse?.Schema?.Type == "object" && successResponse.Schema.Properties != null)
            {
                // Create a synthetic SwaggerDefinition from the response schema
                var outputDefinition = new SwaggerDefinition
                {
                    Type = "object",
                    Properties = new Dictionary<string, SwaggerProperty>(),
                    Required = new List<string>()
                };

                // Add properties from the schema
                foreach (var prop in successResponse.Schema.Properties)
                {
                    outputDefinition.Properties[prop.Key] = new SwaggerProperty
                    {
                        Type = prop.Value.Type,
                        Format = prop.Value.Format,
                        Reference = prop.Value.Reference,
                        Description = $"Response property: {prop.Key}"
                    };
                }

                // Generate DTO if there are any properties
                if (outputDefinition.Properties.Any())
                {
                    return _templateGenerator.GenerateDto($"{CleanOperationName(operationId)}Output", outputDefinition);
                }
            }

            return null;
        }

        private Dictionary<string, SwaggerPathItem> FilterPathsByAllowedPaths(Dictionary<string, SwaggerPathItem> paths)
        {
            if (_settings.apiAllowedPaths == null || _settings.apiAllowedPaths.Length == 0)
            {
                return paths;
            }

            var filteredPaths = new Dictionary<string, SwaggerPathItem>();
            foreach (var path in paths)
            {
                foreach (var allowedPath in _settings.apiAllowedPaths)
                {
                    if (path.Key.Contains(allowedPath))
                    {
                        filteredPaths.Add(path.Key, path.Value);
                        break;
                    }
                }
            }

            return filteredPaths;
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
        /// Cleans and formats operation names for better naming of classes
        /// </summary>
        private string CleanOperationName(string operationId)
        {
            // Для имен с подчеркиванием и дефисами форматируем правильно в PascalCase
            return ToPascalCase(operationId);
        }

        private void CleanupOutputDirectories()
        {
            try
            {
                Debug.Log("Cleaning up output directories before generation...");
                
                // Clean contracts directory
                if (Directory.Exists(_settings.contractsOutFolder))
                {
                    var contractFiles = Directory.GetFiles(_settings.contractsOutFolder, "*.cs");
                    foreach (var file in contractFiles)
                    {
                        File.Delete(file);
                    }
                    Debug.Log($"Deleted {contractFiles.Length} contract files from {_settings.contractsOutFolder}");
                }
                
                // Clean DTO directory
                if (Directory.Exists(_settings.dtoOutFolder))
                {
                    var dtoFiles = Directory.GetFiles(_settings.dtoOutFolder, "*.cs");
                    foreach (var file in dtoFiles)
                    {
                        File.Delete(file);
                    }
                    Debug.Log($"Deleted {dtoFiles.Length} DTO files from {_settings.dtoOutFolder}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error cleaning output directories: {ex.Message}");
            }
        }

        /// <summary>
        /// Собирает все определения схем данных, которые используются в эндпоинтах API
        /// </summary>
        private Dictionary<string, SwaggerDefinition> CollectUsedDefinitions(
            Dictionary<string, SwaggerPathItem> paths,
            Dictionary<string, SwaggerDefinition> allDefinitions)
        {
            if (allDefinitions == null || allDefinitions.Count == 0) 
                return new Dictionary<string, SwaggerDefinition>();

            // Создаем результирующий словарь и набор для отслеживания обработанных схем
            var usedDefinitions = new Dictionary<string, SwaggerDefinition>();
            var processedReferences = new HashSet<string>();
            var pendingReferences = new HashSet<string>();

            // Шаг 1: Собираем прямые ссылки из параметров запросов и ответов
            foreach (var path in paths)
            {
                foreach (var method in path.Value.Methods)
                {
                    var operation = method.Value;
                    
                    // Собираем ссылки из параметров запроса
                    foreach (var param in operation.Parameters)
                    {
                        if (param.Schema != null && !string.IsNullOrEmpty(param.Schema.Reference))
                        {
                            pendingReferences.Add(param.Schema.Reference);
                        }
                    }
                    
                    // Собираем ссылки из ответов
                    foreach (var response in operation.Responses.Values)
                    {
                        if (response.Schema != null && !string.IsNullOrEmpty(response.Schema.Reference))
                        {
                            pendingReferences.Add(response.Schema.Reference);
                        }
                    }
                }
            }

            // Шаг 2: Рекурсивный сбор зависимых схем
            while (pendingReferences.Count > 0)
            {
                var currentRef = pendingReferences.First();
                pendingReferences.Remove(currentRef);
                
                if (processedReferences.Contains(currentRef))
                    continue;
                
                processedReferences.Add(currentRef);
                
                if (allDefinitions.TryGetValue(currentRef, out var definition))
                {
                    usedDefinitions[currentRef] = definition;
                    
                    // Ищем зависимости внутри схемы
                    if (definition.Properties != null)
                    {
                        foreach (var prop in definition.Properties.Values)
                        {
                            // Проверяем прямые ссылки
                            if (!string.IsNullOrEmpty(prop.Reference) && !processedReferences.Contains(prop.Reference))
                            {
                                pendingReferences.Add(prop.Reference);
                            }
                            
                            // Проверяем массивы с ссылками
                            if (prop.Type == "array" && prop.Items != null && 
                                !string.IsNullOrEmpty(prop.Items.Reference) && 
                                !processedReferences.Contains(prop.Items.Reference))
                            {
                                pendingReferences.Add(prop.Items.Reference);
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"Found {usedDefinitions.Count} used definitions out of {allDefinitions.Count} total");
            return usedDefinitions;
        }
    }
} 