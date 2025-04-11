using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Text;
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
            _templateGenerator = new ContractTemplateGenerator(_schemaToClassNameMap, _settings.contractNamespace);
            
            // Debug-логирование для проверки настроек
            Debug.Log($"[DEBUG] SwaggerContractGenerator settings: useResponseDataContainer={settings.useResponseDataContainer}, responseDataField={settings.responseDataField}");
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
                
                // Если нужно использовать контейнер ответа, создаем ResponseDataDTO
                if (_settings.useResponseDataContainer)
                {
                    string responseDataWrapperCode = _templateGenerator.GenerateResponseDataWrapper(_settings.responseDataField);
                    string responseDataWrapperPath = Path.Combine(_settings.dtoOutFolder, "ResponseDataDTO.cs");
                    WriteFileWithTracking(responseDataWrapperPath, responseDataWrapperCode, isDto: true);
                    Debug.Log($"*** Generated response data container: ResponseDataDTO<T> with field {_settings.responseDataField} ***");
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
                    if (string.IsNullOrEmpty(operation.OperationId))
                    {
                        // Пропускаем операции без ID
                        continue;
                    }
                    
                    // Нормализуем ID операции в правильный PascalCase
                    string operationId = CleanOperationName(operation.OperationId);
                    
                    // Формируем имена классов DTO с корректным регистром
                    string inputDtoClassName = $"{operationId}Input"; 
                    string outputDtoClassName = $"{operationId}Output";

                    // Generate Input DTO if needed
                    if (NeedsInputDto(operation) && !processedOperations.Contains(inputDtoClassName))
                    {
                        processedOperations.Add(inputDtoClassName);
                        var inputDto = GenerateOperationInputDto(operationId, operation);
                        
                        if (!string.IsNullOrEmpty(inputDto))
                        {
                            string fileName = $"{inputDtoClassName}.cs";
                            string filePath = Path.Combine(_settings.dtoOutFolder, fileName);
                            WriteFileWithTracking(filePath, inputDto, isDto: true);
                            generatedFiles.Add(filePath);
                        }
                    }

                    // Generate Output DTO if needed
                    if (NeedsOutputDto(operation) && !processedOperations.Contains(outputDtoClassName))
                    {
                        processedOperations.Add(outputDtoClassName);
                        var outputDto = GenerateOperationOutputDto(operationId, operation);
                        
                        if (!string.IsNullOrEmpty(outputDto))
                        {
                            string fileName = $"{outputDtoClassName}.cs";
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
            var processedOperations = new HashSet<string>();

            foreach (var path in paths)
            {
                foreach (var method in path.Value.Methods)
                {
                    var operation = method.Value;
                    if (string.IsNullOrEmpty(operation.OperationId))
                    {
                        Debug.LogWarning($"Operation ID is missing for {method.Key.ToUpper()} {path.Key}. Skipping...");
                        continue;
                    }

                    // Нормализуем ID операции, обеспечивая корректный PascalCase
                    string operationId = CleanOperationName(operation.OperationId);
                    
                    // Skip if this operation ID has already been processed
                    if (processedOperations.Contains(operationId))
                    {
                        Debug.LogWarning($"Duplicate operation ID found: {operationId}. Skipping...");
                        continue;
                    }
                    
                    processedOperations.Add(operationId);
                    
                    // Определяем входной и выходной типы данных
                    bool needsInputDto = NeedsInputDto(operation);
                    bool needsOutputDto = NeedsOutputDto(operation);
                    
                    // Формируем имена DTO файлов с корректным регистром
                    string inputDtoClassName = $"{operationId}Input"; 
                    string outputDtoClassName = $"{operationId}Output";
                    
                    // Генерируем код для input и output DTO, если нужно
                    if (needsInputDto)
                    {
                        string inputDtoCode = GenerateOperationInputDto(operationId, operation);
                        if (!string.IsNullOrEmpty(inputDtoCode))
                        {
                            string fileName = $"{inputDtoClassName}.cs";
                            string filePath = Path.Combine(_settings.dtoOutFolder, fileName);
                            WriteFileWithTracking(filePath, inputDtoCode, isDto: true);
                        }
                    }
                    
                    if (needsOutputDto)
                    {
                        string outputDtoCode = GenerateOperationOutputDto(operationId, operation);
                        if (!string.IsNullOrEmpty(outputDtoCode))
                        {
                            string fileName = $"{outputDtoClassName}.cs";
                            string filePath = Path.Combine(_settings.dtoOutFolder, fileName);
                            WriteFileWithTracking(filePath, outputDtoCode, isDto: true);
                        }
                    }
                    
                    // Получаем типы для контракта с учетом имен DTO с корректным регистром
                    string inputType = needsInputDto ? inputDtoClassName : _templateGenerator.GetInputType(operation);
                    string outputType = needsOutputDto ? outputDtoClassName : _templateGenerator.GetOutputType(operation);
                    
                    // Отладочный лог для отслеживания значений
                    Debug.Log($"[DEBUG] Contract: {operationId}, useResponseDataContainer: {_settings.useResponseDataContainer}, outputType: {outputType}");
                    
                    // Если нужно использовать контейнер для ответа и есть какой-то вывод кроме void или object
                    string contractOutputType = outputType;
                    if (_settings.useResponseDataContainer && outputType != "object" && outputType != "void")
                    {
                        // Получаем обернутый тип для ответа
                        contractOutputType = _templateGenerator.GetResponseContainerType(outputType);
                        Debug.Log($"*** Using response data container for {operationId}: OUTPUT={outputType} → WRAPPED={contractOutputType} ***");
                    }
                    else
                    {
                        Debug.Log($"NOT using container for {operationId} because: useResponseDataContainer={_settings.useResponseDataContainer}, outputType={outputType}");
                    }
                    
                    // Генерируем код контракта с полным URL и типами напрямую
                    string apiUrl = string.Format(_settings.apiTemplate, path.Key.TrimStart('/'));

                    Debug.Log($"[DEBUG] Calling GenerateContract with inputType='{inputType}', contractOutputType='{contractOutputType}'");
                    
                    string contractCode = _templateGenerator.GenerateContract(path.Key, method.Key, operation, apiUrl, inputType, contractOutputType);
                    
                    // Проверка на наличие незамененных шаблонов
                    if (contractCode.Contains("{INPUT_TYPE}") || contractCode.Contains("{OUTPUT_TYPE}"))
                    {
                        Debug.LogError($"Contract still contains template placeholders for {operationId}!");
                    }
                    
                    // Записываем контракт в файл
                    string contractFileName = $"{operationId}Contract.cs";
                    string contractFilePath = Path.Combine(_settings.contractsOutFolder, contractFileName);
                    WriteFileWithTracking(contractFilePath, contractCode, isDto: false);
                    
                    // Отладочный лог - вывод имени файла контракта и типа вывода
                    Debug.Log($"[DEBUG] Generated contract file: {contractFileName} with output type: {contractOutputType}");
                    
                    generatedFiles.Add(contractFilePath);
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
            // Проверяем requestBody (OpenAPI 3.0)
            if (operation.RequestBody?.Schema != null)
            {
                // Если у тела запроса есть прямая ссылка на схему,
                // используем её напрямую и не создаем Input DTO
                if (!string.IsNullOrEmpty(operation.RequestBody.Schema.Reference))
                {
                    return false;
                }
            }

            // Проверяем параметр body (Swagger 2.0)
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

            // Add path and query parameters
            foreach (var param in operation.Parameters.Where(p => p.In == "path" || p.In == "query"))
            {
                // Используем тип из Schema, если она есть
                SwaggerProperty property = new SwaggerProperty
                {
                    Description = param.Description,
                    OriginalName = param.Name
                };
                
                if (param.Schema != null)
                {
                    // Свойства из схемы имеют приоритет
                    property.Type = param.Schema.Type;
                    property.Format = param.Schema.Format;
                    property.Reference = param.Schema.Reference;
                    
                    if (param.Schema.Items != null)
                    {
                        property.Items = new SwaggerProperty
                        {
                            Type = param.Schema.Items.Type,
                            Format = param.Schema.Items.Format,
                            Reference = param.Schema.Items.Reference
                        };
                    }
                }
                else
                {
                    // Используем свойства из самого параметра, если нет схемы
                    property.Type = param.Type;
                    property.Format = param.Format;
                }
                
                inputDefinition.Properties[param.Name] = property;

                if (param.Required)
                {
                    inputDefinition.Required.Add(param.Name);
                }
            }

            // Add body parameter if it's not already a referenced type (Swagger 2.0)
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
                        Description = "From body: " + (prop.Value.Reference ?? ""),
                        OriginalName = prop.Key
                    };
                    
                    // Если свойство в схеме ссылается на другой объект, также сохраняем ссылку
                    if (!string.IsNullOrEmpty(prop.Value.Reference))
                    {
                        inputDefinition.Properties[prop.Key].Reference = prop.Value.Reference;
                    }
                }
            }
            
            // Add requestBody if it's not already a referenced type (OpenAPI 3.0)
            if (operation.RequestBody?.Schema != null && 
                string.IsNullOrEmpty(operation.RequestBody.Schema.Reference) && 
                operation.RequestBody.Schema.Type == "object" && 
                operation.RequestBody.Schema.Properties != null)
            {
                Debug.Log($"Adding properties from requestBody for operation {operationId}");
                foreach (var prop in operation.RequestBody.Schema.Properties)
                {
                    inputDefinition.Properties[prop.Key] = new SwaggerProperty
                    {
                        Type = prop.Value.Type,
                        Format = prop.Value.Format,
                        Description = $"From requestBody: {prop.Key}",
                        OriginalName = prop.Key
                    };
                    
                    // Если свойство в схеме ссылается на другой объект, также сохраняем ссылку
                    if (!string.IsNullOrEmpty(prop.Value.Reference))
                    {
                        inputDefinition.Properties[prop.Key].Reference = prop.Value.Reference;
                    }
                    
                    // Если это массив с ссылкой, также сохраняем её
                    if (prop.Value.Type == "array" && prop.Value.Items != null &&
                        !string.IsNullOrEmpty(prop.Value.Items.Reference))
                    {
                        inputDefinition.Properties[prop.Key].Items = new SwaggerProperty
                        {
                            Reference = prop.Value.Items.Reference
                        };
                    }
                    
                    if (operation.RequestBody.Required)
                    {
                        inputDefinition.Required.Add(prop.Key);
                    }
                }
            }

            // Generate DTO if there are any properties
            if (inputDefinition.Properties.Any())
            {
                // Важно: используем уже нормализованное имя операции - operationId, 
                // которое приходит в метод, а не вызываем CleanOperationName снова
                string className = $"{operationId}Input";
                Debug.Log($"Generating input DTO class: {className}");
                return _templateGenerator.GenerateDto(className, inputDefinition);
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
                        Description = $"Response property: {prop.Key}",
                        OriginalName = prop.Key
                    };
                }

                // Generate DTO if there are any properties
                if (outputDefinition.Properties.Any())
                {
                    // Важно: используем уже нормализованное имя операции
                    string className = $"{operationId}Output";
                    Debug.Log($"Generating output DTO class: {className}");
                    return _templateGenerator.GenerateDto(className, outputDefinition);
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
            if (string.IsNullOrEmpty(operationId))
                return string.Empty;
            
            // Для имен с подчеркиванием и дефисами форматируем правильно в PascalCase
            string pascalCase = ToPascalCase(operationId);
            
            // Специальная обработка распространенных префиксов методов
            string[] commonPrefixes = new[] { "get", "post", "put", "delete", "patch" };
            
            foreach (var prefix in commonPrefixes)
            {
                // Если operationId начинается с [prefix][rest], например getClientProfile, 
                // преобразуем в Get[Rest] => GetClientProfile
                if (pascalCase.Length > prefix.Length && 
                    pascalCase.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    char.IsUpper(pascalCase[prefix.Length]))
                {
                    string prefixProperCase = char.ToUpperInvariant(prefix[0]) + prefix.Substring(1).ToLowerInvariant();
                    string restOfName = pascalCase.Substring(prefix.Length);
                    
                    // Убедимся, что первая буква restOfName уже заглавная
                    if (char.IsUpper(restOfName[0]))
                    {
                        pascalCase = prefixProperCase + restOfName;
                    }
                }
            }
            
            // Удалить потенциальные суффиксы "Get", "Post" и т.д.
            // Например, ClientProfileGet => ClientProfile
            foreach (var suffix in commonPrefixes)
            {
                string suffixProperCase = char.ToUpperInvariant(suffix[0]) + suffix.Substring(1).ToLowerInvariant();
                if (pascalCase.EndsWith(suffixProperCase))
                {
                    // Не удаляем суффикс, если после удаления получается пустая строка
                    string withoutSuffix = pascalCase.Substring(0, pascalCase.Length - suffixProperCase.Length);
                    if (!string.IsNullOrEmpty(withoutSuffix))
                    {
                        pascalCase = withoutSuffix;
                    }
                }
            }
            
            // Гарантируем, что первая буква заглавная
            if (pascalCase.Length > 0 && !char.IsUpper(pascalCase[0]))
            {
                pascalCase = char.ToUpperInvariant(pascalCase[0]) + pascalCase.Substring(1);
            }
            
            Debug.Log($"CleanOperationName: {operationId} => {pascalCase}");
            return pascalCase;
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

            Debug.Log($"Starting to collect used definitions from {paths.Count} paths, total definitions: {allDefinitions.Count}");
            
            // Шаг 1: Собираем прямые ссылки из параметров запросов и ответов
            foreach (var path in paths)
            {
                foreach (var method in path.Value.Methods)
                {
                    var operation = method.Value;
                    string operationId = CleanOperationName(operation.OperationId);
                    
                    // Собираем ссылки из параметров запроса
                    foreach (var param in operation.Parameters)
                    {
                        if (param.Schema != null)
                        {
                            if (!string.IsNullOrEmpty(param.Schema.Reference))
                            {
                                string normalizedRef = NormalizeReference(param.Schema.Reference);
                                pendingReferences.Add(normalizedRef);
                                Debug.Log($"Added parameter schema reference: {normalizedRef} from {method.Key} {path.Key}");
                            }
                            
                            // Проверяем ссылки в массивах
                            if (param.Schema.Type == "array" && param.Schema.Items != null && 
                                !string.IsNullOrEmpty(param.Schema.Items.Reference))
                            {
                                string normalizedRef = NormalizeReference(param.Schema.Items.Reference);
                                pendingReferences.Add(normalizedRef);
                                Debug.Log($"Added parameter array item reference: {normalizedRef} from {method.Key} {path.Key}");
                            }
                        }
                    }
                    
                    // Собираем ссылки из requestBody (OpenAPI 3.0)
                    if (operation.RequestBody?.Schema != null)
                    {
                        if (!string.IsNullOrEmpty(operation.RequestBody.Schema.Reference))
                        {
                            string normalizedRef = NormalizeReference(operation.RequestBody.Schema.Reference);
                            pendingReferences.Add(normalizedRef);
                            Debug.Log($"Added requestBody reference: {normalizedRef} from {method.Key} {path.Key}");
                        }
                        
                        // Проверяем свойства схемы запроса
                        if (operation.RequestBody.Schema.Properties != null)
                        {
                            foreach (var prop in operation.RequestBody.Schema.Properties.Values)
                            {
                                if (!string.IsNullOrEmpty(prop.Reference))
                                {
                                    string normalizedRef = NormalizeReference(prop.Reference);
                                    pendingReferences.Add(normalizedRef);
                                    Debug.Log($"Added requestBody property reference: {normalizedRef} from {method.Key} {path.Key}");
                                }
                                
                                // Проверяем ссылки в массивах
                                if (prop.Type == "array" && prop.Items != null && 
                                    !string.IsNullOrEmpty(prop.Items.Reference))
                                {
                                    string normalizedRef = NormalizeReference(prop.Items.Reference);
                                    pendingReferences.Add(normalizedRef);
                                    Debug.Log($"Added requestBody array item reference: {normalizedRef} from {method.Key} {path.Key}");
                                }
                            }
                        }
                    }
                    
                    // Собираем ссылки из ответов
                    foreach (var response in operation.Responses.Values)
                    {
                        if (response.Schema != null)
                        {
                            // Проверяем прямую ссылку
                            if (!string.IsNullOrEmpty(response.Schema.Reference))
                            {
                                string normalizedRef = NormalizeReference(response.Schema.Reference);
                                pendingReferences.Add(normalizedRef);
                                Debug.Log($"Added response schema reference: {normalizedRef} from {method.Key} {path.Key}");
                            }
                            
                            // Проверяем ссылки в массивах
                            if (response.Schema.Type == "array" && response.Schema.Items != null && 
                                !string.IsNullOrEmpty(response.Schema.Items.Reference))
                            {
                                string normalizedRef = NormalizeReference(response.Schema.Items.Reference);
                                pendingReferences.Add(normalizedRef);
                                Debug.Log($"Added response array item reference: {normalizedRef} from {method.Key} {path.Key}");
                            }
                            
                            // Проверяем свойства ответа
                            if (response.Schema.Properties != null)
                            {
                                foreach (var prop in response.Schema.Properties)
                                {
                                    // Проверяем все поля на предмет ссылок
                                    if (!string.IsNullOrEmpty(prop.Value.Reference))
                                    {
                                        string normalizedRef = NormalizeReference(prop.Value.Reference);
                                        pendingReferences.Add(normalizedRef);
                                        Debug.Log($"Added response property reference: {normalizedRef} from field '{prop.Key}' in {method.Key} {path.Key}");
                                    }
                                    
                                    // Проверяем ссылки в массивах
                                    if (prop.Value.Type == "array" && prop.Value.Items != null && 
                                        !string.IsNullOrEmpty(prop.Value.Items.Reference))
                                    {
                                        string normalizedRef = NormalizeReference(prop.Value.Items.Reference);
                                        pendingReferences.Add(normalizedRef);
                                        Debug.Log($"Added response array item reference: {normalizedRef} from field '{prop.Key}' in {method.Key} {path.Key}");
                                    }
                                    
                                    // Всегда проверяем поле 'data', вне зависимости от настройки useResponseDataContainer
                                    if (prop.Key == "data" || prop.Value.OriginalName == "data")
                                    {
                                        // Если поле data имеет свойства, анализируем их
                                        if (prop.Value.Properties != null)
                                        {
                                            foreach (var dataProp in prop.Value.Properties)
                                            {
                                                if (!string.IsNullOrEmpty(dataProp.Value.Reference))
                                                {
                                                    string normalizedRef = NormalizeReference(dataProp.Value.Reference);
                                                    pendingReferences.Add(normalizedRef);
                                                    Debug.Log($"Added data field property reference: {normalizedRef} from {method.Key} {path.Key}");
                                                }
                                                
                                                if (dataProp.Value.Type == "array" && dataProp.Value.Items != null && 
                                                    !string.IsNullOrEmpty(dataProp.Value.Items.Reference))
                                                {
                                                    string normalizedRef = NormalizeReference(dataProp.Value.Items.Reference);
                                                    pendingReferences.Add(normalizedRef);
                                                    Debug.Log($"Added data field array item reference: {normalizedRef} from {method.Key} {path.Key}");
                                                }
                                            }
                                        }
                                        
                                        // Если поле data не имеет свойств или ссылок, пытаемся угадать по имени операции
                                        if ((prop.Value.Properties == null || !prop.Value.Properties.Any()) && 
                                            string.IsNullOrEmpty(prop.Value.Reference))
                                        {
                                            // Если мы нашли поле data без явной ссылки, ищем подходящие схемы по названию операции
                                            string searchOperationName = operationId.Replace("Get", "").Replace("Post", "").Replace("Put", "").Replace("Patch", "").Replace("Delete", "");
                                            
                                            foreach (var defKey in allDefinitions.Keys)
                                            {
                                                if (defKey.Contains(searchOperationName) && (defKey.EndsWith("ResponseDTO") || defKey.EndsWith("Response")))
                                                {
                                                    pendingReferences.Add(defKey);
                                                    Debug.Log($"Added potential data field match for {operationId}: {defKey}");
                                                }
                                            }
                                            
                                            // Добавляем специальный случай для UpdateCurrencyResponseDTO
                                            if (operationId.Contains("Currency") || operationId.Contains("Profile"))
                                            {
                                                foreach (var defKey in allDefinitions.Keys)
                                                {
                                                    if (defKey.Contains("UpdateCurrencyResponseDTO"))
                                                    {
                                                        pendingReferences.Add(defKey);
                                                        Debug.Log($"Added special case UpdateCurrencyResponseDTO for {operationId}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Выведем все ожидающие обработки ссылки
            Debug.Log($"Pending references count: {pendingReferences.Count}");
            foreach (var reference in pendingReferences)
            {
                Debug.Log($"Pending reference: {reference}");
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
                    Debug.Log($"Added definition to used: {currentRef}, Title: {definition.Title}");
                    
                    // Ищем зависимости внутри схемы
                    if (definition.Properties != null)
                    {
                        foreach (var prop in definition.Properties.Values)
                        {
                            // Проверяем прямые ссылки
                            if (!string.IsNullOrEmpty(prop.Reference) && !processedReferences.Contains(prop.Reference))
                            {
                                string normalizedRef = NormalizeReference(prop.Reference);
                                pendingReferences.Add(normalizedRef);
                                Debug.Log($"Added property reference dependency: {normalizedRef} from {currentRef}");
                            }
                            
                            // Проверяем массивы с ссылками
                            if (prop.Type == "array" && prop.Items != null && 
                                !string.IsNullOrEmpty(prop.Items.Reference))
                            {
                                string normalizedRef = NormalizeReference(prop.Items.Reference);
                                if (!processedReferences.Contains(normalizedRef))
                                {
                                    pendingReferences.Add(normalizedRef);
                                    Debug.Log($"Added array item reference dependency: {normalizedRef} from {currentRef}");
                                }
                            }
                            
                            // Проверяем вложенные свойства
                            if (prop.Properties != null)
                            {
                                foreach (var nestedProp in prop.Properties.Values)
                                {
                                    if (!string.IsNullOrEmpty(nestedProp.Reference))
                                    {
                                        string normalizedRef = NormalizeReference(nestedProp.Reference);
                                        if (!processedReferences.Contains(normalizedRef))
                                        {
                                            pendingReferences.Add(normalizedRef);
                                            Debug.Log($"Added nested property reference dependency: {normalizedRef} from {currentRef}");
                                        }
                                    }
                                    
                                    // Проверяем вложенные массивы
                                    if (nestedProp.Type == "array" && nestedProp.Items != null && 
                                        !string.IsNullOrEmpty(nestedProp.Items.Reference))
                                    {
                                        string normalizedRef = NormalizeReference(nestedProp.Items.Reference);
                                        if (!processedReferences.Contains(normalizedRef))
                                        {
                                            pendingReferences.Add(normalizedRef);
                                            Debug.Log($"Added nested array item reference dependency: {normalizedRef} from {currentRef}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Referenced schema not found in definitions: {currentRef}");
                }
            }
            
            Debug.Log($"Found {usedDefinitions.Count} used definitions out of {allDefinitions.Count} total");
            return usedDefinitions;
        }

        // Расширенная версия нормализации ссылок для CollectUsedDefinitions
        private string NormalizeReference(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return reference;

            // Обрабатываем стандартные префиксы ссылок
            if (reference.StartsWith("#/definitions/"))
                return reference.Substring("#/definitions/".Length);
            
            if (reference.StartsWith("#/components/schemas/"))
                return reference.Substring("#/components/schemas/".Length);
            
            // Remove any version information from the reference
            int versionIndex = reference.IndexOf('?');
            if (versionIndex >= 0)
            {
                reference = reference.Substring(0, versionIndex);
            }
            
            return reference;
        }

        private string GenerateOperationDtoClass(string operationId, string dtoType, List<SwaggerParameter> parameters)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine($"using Newtonsoft.Json;");
            sb.AppendLine($"using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {_settings.contractNamespace}.Dto");
            sb.AppendLine($"{{");
            sb.AppendLine($"    [Serializable]");
            sb.AppendLine($"    public class {operationId}{dtoType}");
            sb.AppendLine($"    {{");
            
            foreach (var param in parameters)
            {
                string propertyType = GetParameterType(param);
                string propertyName = ToPascalCase(param.Name);
                
                // Добавляем JsonProperty атрибут, если имя свойства в API отличается от имени свойства в C#
                if (!string.IsNullOrEmpty(param.OriginalName) && param.OriginalName != propertyName)
                {
                    sb.AppendLine($"        [JsonProperty(\"{param.OriginalName}\")]");
                }
                
                if (!string.IsNullOrEmpty(param.Description))
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// {param.Description}");
                    
                    if (param.Required)
                    {
                        sb.AppendLine($"        /// Required: true");
                    }
                    
                    sb.AppendLine($"        /// </summary>");
                }
                
                // Добавляем атрибут SerializeField для отображения в Unity Inspector
                sb.AppendLine($"        [field: SerializeField]");
                
                sb.AppendLine($"        public {propertyType} {propertyName} {{ get; set; }}");
                sb.AppendLine();
            }
            
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Определяет тип параметра для использования в свойствах DTO
        /// </summary>
        private string GetParameterType(SwaggerParameter param)
        {
            if (param.Schema != null)
            {
                if (!string.IsNullOrEmpty(param.Schema.Reference))
                {
                    return _schemaToClassNameMap.TryGetValue(param.Schema.Reference, out var className) 
                        ? className 
                        : param.Schema.Reference;
                }
                
                if (param.Schema.Type == "array")
                {
                    if (param.Schema.Items != null && !string.IsNullOrEmpty(param.Schema.Items.Reference))
                    {
                        string itemType = _schemaToClassNameMap.TryGetValue(param.Schema.Items.Reference, out var className) 
                            ? className 
                            : param.Schema.Items.Reference;
                        return $"List<{itemType}>";
                    }
                    return $"List<{MapSwaggerTypeToCs(param.Schema.Items?.Type, param.Schema.Items?.Format)}>";
                }
                
                return MapSwaggerTypeToCs(param.Schema.Type, param.Schema.Format);
            }
            
            return MapSwaggerTypeToCs(param.Type, param.Format);
        }

        /// <summary>
        /// Преобразует Swagger типы в C# типы
        /// </summary>
        private string MapSwaggerTypeToCs(string type, string format)
        {
            if (string.IsNullOrEmpty(type))
                return "object";
            
            switch (type.ToLower())
            {
                case "string":
                    if (format == "date-time")
                        return "DateTime";
                    if (format == "date")
                        return "DateTime";
                    if (format == "byte")
                        return "byte[]";
                    return "string";
                case "integer":
                    if (format == "int64")
                        return "long";
                    return "int";
                case "number":
                    if (format == "float")
                        return "float";
                    if (format == "double")
                        return "double";
                    return "decimal";
                case "boolean":
                    return "bool";
                case "array":
                    return "List<object>";
                case "object":
                    return "object";
                default:
                    return "object";
            }
        }
    }
} 