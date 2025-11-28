namespace Game.Modules.unity.meta.service.Modules.WebProvider.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;

    /// <summary>
    /// Editor window for OpenAPI contract generation
    /// 
    /// Features:
    /// - Configure OpenAPI settings
    /// - Generate contracts from Swagger/OpenAPI JSON
    /// - Manage output folders and namespaces
    /// - Preview generation settings
    /// 
    /// Usage:
    /// 1. Open via UniGame > Meta Backend > OpenApi Generator
    /// 2. Configure settings or select existing asset
    /// 3. Click Generate Contracts to create C# classes
    /// </summary>
    public class OpenApiGeneratorEditorWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "OpenAPI Contract Generator";
        
        // UI Elements
        private VisualElement _root;
        private ObjectField _settingsAssetField;
        private TextField _apiJsonPathField;
        private Button _browseJsonButton;
        private TextField _contractsOutFolderField;
        private Button _browseContractsButton;
        private TextField _dtoOutFolderField;
        private Button _browseDtoButton;
        private TextField _contractNamespaceField;
        private TextField _apiTemplateField;
        private TextField _allowedPathsField;
        private Toggle _cleanUpToggle;
        private Toggle _useResponseContainerToggle;
        private TextField _responseDataFieldField;
        private Button _generateButton;
        private Button _saveSettingsButton;
        private Label _statusLabel;
        
        // Data
        private OpenApiSettingsAsset _settingsAsset;
        private string _defaultSettingsPath;

        [MenuItem("UniGame/Meta Backend/OpenApi Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<OpenApiGeneratorEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(600, 700);
        }

        public void CreateGUI()
        {
            _root = rootVisualElement;
            
            // Load UXML
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var editorFolder = Path.GetDirectoryName(scriptPath);
            var uxmlPath = Path.Combine(editorFolder, "OpenApiGeneratorEditorWindow.uxml");
            var ussPath = Path.Combine(editorFolder, "OpenApiGeneratorEditorWindow.uss");
            
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree != null)
            {
                visualTree.CloneTree(_root);
                
                // Load USS
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (styleSheet != null)
                {
                    _root.styleSheets.Add(styleSheet);
                }
            }
            else
            {
                Debug.LogError($"Failed to load UXML from: {uxmlPath}");
                CreateFallbackUI();
                return;
            }
            
            // Find default settings path
            FindDefaultSettingsPath();
            
            // Load or create settings
            LoadOrCreateSettings();
            
            // Cache UI elements
            CacheUIElements();
            
            // Setup event handlers
            SetupEventHandlers();
            
            // Load settings into UI
            RefreshUI();
        }
        
        private void CacheUIElements()
        {
            _settingsAssetField = _root.Q<ObjectField>("settings-asset-field");
            _apiJsonPathField = _root.Q<TextField>("api-json-path-field");
            _browseJsonButton = _root.Q<Button>("browse-json-button");
            _contractsOutFolderField = _root.Q<TextField>("contracts-out-folder-field");
            _browseContractsButton = _root.Q<Button>("browse-contracts-button");
            _dtoOutFolderField = _root.Q<TextField>("dto-out-folder-field");
            _browseDtoButton = _root.Q<Button>("browse-dto-button");
            _contractNamespaceField = _root.Q<TextField>("contract-namespace-field");
            _apiTemplateField = _root.Q<TextField>("api-template-field");
            _allowedPathsField = _root.Q<TextField>("allowed-paths-field");
            _cleanUpToggle = _root.Q<Toggle>("clean-up-toggle");
            _useResponseContainerToggle = _root.Q<Toggle>("use-response-container-toggle");
            _responseDataFieldField = _root.Q<TextField>("response-data-field-field");
            _generateButton = _root.Q<Button>("generate-button");
            _saveSettingsButton = _root.Q<Button>("save-settings-button");
            _statusLabel = _root.Q<Label>("status-label");
        }
        
        private void SetupEventHandlers()
        {
            if (_settingsAssetField != null)
            {
                _settingsAssetField.objectType = typeof(OpenApiSettingsAsset);
                _settingsAssetField.RegisterValueChangedCallback(evt =>
                {
                    _settingsAsset = evt.newValue as OpenApiSettingsAsset;
                    RefreshUI();
                });
            }
            
            if (_apiJsonPathField != null)
                _apiJsonPathField.RegisterValueChangedCallback(evt => MarkDirty());
            
            if (_browseJsonButton != null)
                _browseJsonButton.clicked += BrowseForJsonFile;
            
            if (_contractsOutFolderField != null)
                _contractsOutFolderField.RegisterValueChangedCallback(evt => MarkDirty());
            
            if (_browseContractsButton != null)
                _browseContractsButton.clicked += () => BrowseForFolder(true);
            
            if (_dtoOutFolderField != null)
                _dtoOutFolderField.RegisterValueChangedCallback(evt => MarkDirty());
            
            if (_browseDtoButton != null)
                _browseDtoButton.clicked += () => BrowseForFolder(false);
            
            if (_contractNamespaceField != null)
                _contractNamespaceField.RegisterValueChangedCallback(evt => MarkDirty());
            
            if (_apiTemplateField != null)
                _apiTemplateField.RegisterValueChangedCallback(evt => MarkDirty());
            
            if (_allowedPathsField != null)
                _allowedPathsField.RegisterValueChangedCallback(evt => MarkDirty());
            
            if (_cleanUpToggle != null)
                _cleanUpToggle.RegisterValueChangedCallback(evt => MarkDirty());
            
            if (_useResponseContainerToggle != null)
            {
                _useResponseContainerToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_responseDataFieldField != null)
                        _responseDataFieldField.SetEnabled(evt.newValue);
                    MarkDirty();
                });
            }
            
            if (_responseDataFieldField != null)
                _responseDataFieldField.RegisterValueChangedCallback(evt => MarkDirty());
            
            if (_generateButton != null)
                _generateButton.clicked += GenerateContracts;
            
            if (_saveSettingsButton != null)
                _saveSettingsButton.clicked += SaveSettings;
            
            var createSettingsButton = _root.Q<Button>("create-settings-button");
            if (createSettingsButton != null)
                createSettingsButton.clicked += CreateNewSettingsAsset;
            
            var selectSettingsButton = _root.Q<Button>("select-settings-button");
            if (selectSettingsButton != null)
                selectSettingsButton.clicked += SelectSettingsAsset;
        }
        
        private void CreateFallbackUI()
        {
            var label = new Label("Failed to load UI. Please ensure OpenApiGeneratorEditorWindow.uxml is in the correct location.");
            label.style.paddingTop = 20;
            label.style.paddingBottom = 20;
            label.style.paddingLeft = 20;
            label.style.paddingRight = 20;
            label.style.color = new StyleColor(Color.red);
            _root.Add(label);
        }

        private void FindDefaultSettingsPath()
        {
            // Try to find ContractsConfigurationAsset to save settings nearby
            var configAssets = AssetDatabase.FindAssets("t:ContractsConfigurationAsset");
            if (configAssets.Length > 0)
            {
                var configPath = AssetDatabase.GUIDToAssetPath(configAssets[0]);
                var configDir = Path.GetDirectoryName(configPath);
                _defaultSettingsPath = configDir;
            }
            else
            {
                _defaultSettingsPath = "Assets/UniGame.Generated/OpenApi";
            }
        }

        private void LoadOrCreateSettings()
        {
            // Try to find existing OpenApiSettingsAsset
            var guids = AssetDatabase.FindAssets("t:OpenApiSettingsAsset");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settingsAsset = AssetDatabase.LoadAssetAtPath<OpenApiSettingsAsset>(path);
            }
        }

        private void RefreshUI()
        {
            if (_settingsAsset == null || _settingsAsset.apiSettings == null)
            {
                SetDefaultValues();
                return;
            }
            
            var settings = _settingsAsset.apiSettings;
            
            _apiJsonPathField.value = settings.apiJsonPath;
            _contractsOutFolderField.value = settings.contractsOutFolder;
            _dtoOutFolderField.value = settings.dtoOutFolder;
            _contractNamespaceField.value = settings.contractNamespace;
            _apiTemplateField.value = settings.apiTemplate;
            _allowedPathsField.value = settings.apiAllowedPaths != null ? string.Join(", ", settings.apiAllowedPaths) : "";
            _cleanUpToggle.value = settings.cleanUpOnGenerate;
            _useResponseContainerToggle.value = settings.useResponseDataContainer;
            _responseDataFieldField.value = settings.responseDataField;
            _responseDataFieldField.SetEnabled(settings.useResponseDataContainer);
            
            UpdateStatus($"Loaded settings from: {AssetDatabase.GetAssetPath(_settingsAsset)}");
        }

        private void SetDefaultValues()
        {
            _apiJsonPathField.value = "";
            _contractsOutFolderField.value = "Assets/UniGame.Generated/WebContracts/";
            _dtoOutFolderField.value = "Assets/UniGame.Generated/WebContracts/DTO/";
            _contractNamespaceField.value = "Game.Generated.WebContracts";
            _apiTemplateField.value = "api/{0}";
            _allowedPathsField.value = "";
            _cleanUpToggle.value = false;
            _useResponseContainerToggle.value = false;
            _responseDataFieldField.value = "data";
            _responseDataFieldField.SetEnabled(false);
            
            UpdateStatus("No settings asset loaded. Using default values.");
        }

        private void CreateNewSettingsAsset()
        {
            // Ensure directory exists
            if (!Directory.Exists(_defaultSettingsPath))
            {
                Directory.CreateDirectory(_defaultSettingsPath);
            }
            
            // Create new settings asset
            var newAsset = CreateInstance<OpenApiSettingsAsset>();
            newAsset.apiSettings = new OpenApiSettings
            {
                apiJsonPath = _apiJsonPathField.value,
                contractsOutFolder = _contractsOutFolderField.value,
                dtoOutFolder = _dtoOutFolderField.value,
                contractNamespace = _contractNamespaceField.value,
                apiTemplate = _apiTemplateField.value,
                apiAllowedPaths = ParseAllowedPaths(_allowedPathsField.value),
                cleanUpOnGenerate = _cleanUpToggle.value,
                useResponseDataContainer = _useResponseContainerToggle.value,
                responseDataField = _responseDataFieldField.value
            };
            
            // Generate unique path
            string assetPath = Path.Combine(_defaultSettingsPath, "OpenApiSettings.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            // Create asset
            AssetDatabase.CreateAsset(newAsset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _settingsAsset = newAsset;
            _settingsAssetField.value = _settingsAsset;
            
            UpdateStatus($"Created new settings asset at: {assetPath}");
            
            // Select the asset in the project window
            EditorGUIUtility.PingObject(newAsset);
        }

        private void SelectSettingsAsset()
        {
            var path = EditorUtility.OpenFilePanel("Select OpenAPI Settings Asset", "Assets", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                
                var asset = AssetDatabase.LoadAssetAtPath<OpenApiSettingsAsset>(path);
                if (asset != null)
                {
                    _settingsAsset = asset;
                    _settingsAssetField.value = _settingsAsset;
                    RefreshUI();
                }
                else
                {
                    UpdateStatus("Selected file is not an OpenApiSettingsAsset", true);
                }
            }
        }

        private void SaveSettings()
        {
            if (_settingsAsset == null)
            {
                if (EditorUtility.DisplayDialog("No Settings Asset", 
                    "No settings asset is loaded. Create a new one?", "Yes", "No"))
                {
                    CreateNewSettingsAsset();
                    return;
                }
                return;
            }
            
            // Update settings from UI
            _settingsAsset.apiSettings.apiJsonPath = _apiJsonPathField.value;
            _settingsAsset.apiSettings.contractsOutFolder = _contractsOutFolderField.value;
            _settingsAsset.apiSettings.dtoOutFolder = _dtoOutFolderField.value;
            _settingsAsset.apiSettings.contractNamespace = _contractNamespaceField.value;
            _settingsAsset.apiSettings.apiTemplate = _apiTemplateField.value;
            _settingsAsset.apiSettings.apiAllowedPaths = ParseAllowedPaths(_allowedPathsField.value);
            _settingsAsset.apiSettings.cleanUpOnGenerate = _cleanUpToggle.value;
            _settingsAsset.apiSettings.useResponseDataContainer = _useResponseContainerToggle.value;
            _settingsAsset.apiSettings.responseDataField = _responseDataFieldField.value;
            
            EditorUtility.SetDirty(_settingsAsset);
            AssetDatabase.SaveAssets();
            
            UpdateStatus("Settings saved successfully");
        }

        private void GenerateContracts()
        {
            if (_settingsAsset == null)
            {
                UpdateStatus("Please create or select a settings asset first", true);
                return;
            }
            
            // Save settings before generating
            SaveSettings();
            
            // Validate settings
            if (string.IsNullOrEmpty(_settingsAsset.apiSettings.apiJsonPath))
            {
                UpdateStatus("API JSON path is not specified", true);
                return;
            }
            
            if (!File.Exists(_settingsAsset.apiSettings.apiJsonPath))
            {
                UpdateStatus($"API JSON file not found: {_settingsAsset.apiSettings.apiJsonPath}", true);
                return;
            }
            
            try
            {
                UpdateStatus("Generating contracts...");
                
                // Generate contracts
                WebApiGenerator.GenerateContracts(_settingsAsset.apiSettings);
                
                UpdateStatus("✓ Contracts generated successfully!");
                
                // Refresh asset database
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error generating contracts: {ex.Message}", true);
                Debug.LogException(ex);
            }
        }

        private void BrowseForJsonFile()
        {
            var currentPath = string.IsNullOrEmpty(_apiJsonPathField.value) 
                ? Application.dataPath 
                : Path.GetDirectoryName(_apiJsonPathField.value);
                
            var path = EditorUtility.OpenFilePanel("Select Swagger/OpenAPI JSON", currentPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                _apiJsonPathField.value = path;
                MarkDirty();
            }
        }

        private void BrowseForFolder(bool isContracts)
        {
            var currentPath = isContracts ? _contractsOutFolderField.value : _dtoOutFolderField.value;
            if (string.IsNullOrEmpty(currentPath))
            {
                currentPath = "Assets";
            }
            
            var absolutePath = Path.Combine(Application.dataPath, currentPath.Replace("Assets/", ""));
            var path = EditorUtility.OpenFolderPanel(
                isContracts ? "Select Contracts Output Folder" : "Select DTO Output Folder", 
                absolutePath, 
                "");
                
            if (!string.IsNullOrEmpty(path))
            {
                // Convert to relative path
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                
                // Ensure trailing slash
                if (!path.EndsWith("/"))
                {
                    path += "/";
                }
                
                if (isContracts)
                {
                    _contractsOutFolderField.value = path;
                }
                else
                {
                    _dtoOutFolderField.value = path;
                }
                
                MarkDirty();
            }
        }

        private string[] ParseAllowedPaths(string pathsString)
        {
            if (string.IsNullOrWhiteSpace(pathsString))
            {
                return Array.Empty<string>();
            }
            
            return pathsString
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToArray();
        }

        private void MarkDirty()
        {
            if (_settingsAsset != null)
            {
                EditorUtility.SetDirty(_settingsAsset);
            }
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            if (_statusLabel == null) return;
            
            _statusLabel.text = message;
            _statusLabel.RemoveFromClassList("status-error");
            _statusLabel.RemoveFromClassList("status-success");
            
            if (isError)
            {
                _statusLabel.AddToClassList("status-error");
            }
            else if (message.Contains("✓") || message.Contains("success"))
            {
                _statusLabel.AddToClassList("status-success");
            }
                
            Debug.Log($"[OpenAPI Generator] {message}");
        }
    }
}
