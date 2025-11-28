namespace MetaService.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Game.Modules.ModelMapping;
    using MetaService.Runtime;
    using Newtonsoft.Json;
    using R3;
    using UniGame.MetaBackend.Runtime;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Editor window for monitoring Meta Backend contract execution in real-time.
    /// 
    /// Features:
    /// - Configuration display: View all registered contracts with their settings
    /// - Contract history: Browse execution history with search and filtering
    /// - Real-time stream: Live updates as contracts execute
    /// - Export: Save history to JSON for analysis
    /// 
    /// Usage:
    /// 1. Open via UniGame > Meta Backend > Meta Editor Window
    /// 2. Start Play Mode to connect to BackendMetaService.EditorInstance
    /// 3. Execute contracts in your game to see real-time updates
    /// 
    /// The window automatically subscribes to:
    /// - BackendMetaService.ContractHistory (full execution history)
    /// - BackendMetaService.DataStream (real-time contract results)
    /// </summary>
    public class MetaBackendEditorWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "Meta Backend Monitor";
        private const int REFRESH_RATE_MS = 500;
        
        // UI Elements
        private VisualElement _root;
        private Toggle _autoRefreshToggle;
        private Button _refreshButton;
        private Button _clearHistoryButton;
        private Button _exportButton;
        private Label _configStatus;
        private VisualElement _configDetails;
        private TextField _historySearch;
        private VisualElement _historyContainer;
        private Label _streamStatus;
        private VisualElement _streamContainer;
        private ScrollView _streamScroll;
        
        // Data
        private BackendMetaService _service;
        private ContractsConfigurationAsset _configurationAsset;
        private IDisposable _streamSubscription;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private List<ContractDataResult> _streamResults = new();
        private int _maxStreamItems = 50;
        private string _currentSearchFilter = string.Empty;

        [MenuItem("UniGame/Meta Backend/Meta Editor Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<MetaBackendEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(800, 600);
        }

        public void CreateGUI()
        {
            _root = rootVisualElement;
            
            // Load UXML using relative path from this script's location
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var editorFolder = System.IO.Path.GetDirectoryName(scriptPath);
            var uxmlPath = System.IO.Path.Combine(editorFolder, "MetaBackendEditorWindow.uxml");
            
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            
            if (visualTree != null)
            {
                visualTree.CloneTree(_root);
            }
            else
            {
                Debug.LogError($"Failed to load MetaBackendEditorWindow.uxml from path: {uxmlPath}");
                CreateFallbackUI();
                return;
            }
            
            // Cache UI elements
            CacheUIElements();
            
            // Setup event handlers
            SetupEventHandlers();
            
            // Load configuration
            LoadConfiguration();
            
            // Initial refresh
            RefreshAll();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _streamSubscription?.Dispose();
            _streamSubscription = null;
        }

        private void CacheUIElements()
        {
            _autoRefreshToggle = _root.Q<Toggle>("auto-refresh-toggle");
            _refreshButton = _root.Q<Button>("refresh-button");
            _clearHistoryButton = _root.Q<Button>("clear-history-button");
            _exportButton = _root.Q<Button>("export-button");
            _configStatus = _root.Q<Label>("config-status");
            _configDetails = _root.Q<VisualElement>("config-details");
            _historySearch = _root.Q<TextField>("history-search");
            _historyContainer = _root.Q<VisualElement>("history-container");
            _streamStatus = _root.Q<Label>("stream-status");
            _streamContainer = _root.Q<VisualElement>("stream-container");
            _streamScroll = _root.Q<ScrollView>("stream-scroll");
        }

        private void SetupEventHandlers()
        {
            if (_autoRefreshToggle != null)
            {
                _autoRefreshToggle.value = _autoRefresh;
                _autoRefreshToggle.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
            }

            if (_refreshButton != null)
            {
                _refreshButton.clicked += RefreshAll;
            }

            if (_clearHistoryButton != null)
            {
                _clearHistoryButton.clicked += ClearHistory;
            }

            if (_exportButton != null)
            {
                _exportButton.clicked += ExportHistory;
            }

            if (_historySearch != null)
            {
                _historySearch.RegisterValueChangedCallback(evt =>
                {
                    _currentSearchFilter = evt.newValue;
                    RefreshHistory();
                });
            }
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh) return;
            
            var currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - _lastRefreshTime < REFRESH_RATE_MS / 1000.0) return;
            
            _lastRefreshTime = currentTime;
            
            // Check if service instance changed
            var currentService = BackendMetaService.EditorInstance;
            if (currentService != _service)
            {
                _service = currentService;
                OnServiceChanged();
            }
            
            // Refresh history
            if (_service != null)
            {
                RefreshHistory();
            }
        }

        private void OnServiceChanged()
        {
            // Unsubscribe from old service
            _streamSubscription?.Dispose();
            _streamSubscription = null;
            _streamResults.Clear();
            
            if (_service != null)
            {
                // Subscribe to data stream
                try
                {
                    _streamSubscription = _service
                        .DataStream
                        .Subscribe(OnStreamData);
                    
                    UpdateStreamStatus(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to subscribe to DataStream: {e.Message}");
                    UpdateStreamStatus(false);
                }
            }
            else
            {
                UpdateStreamStatus(false);
            }
            
            RefreshAll();
        }

        private void OnStreamData(ContractDataResult result)
        {
            if (result == null) return;
            
            _streamResults.Insert(0, result);
            
            // Limit stream size
            if (_streamResults.Count > _maxStreamItems)
            {
                _streamResults.RemoveAt(_streamResults.Count - 1);
            }
            
            // Update UI on main thread
            EditorApplication.delayCall += RefreshStream;
        }

        private void LoadConfiguration()
        {
            // Try to find configuration asset
            var guids = AssetDatabase.FindAssets("t:ContractsConfigurationAsset");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _configurationAsset = AssetDatabase.LoadAssetAtPath<ContractsConfigurationAsset>(path);
            }
        }

        private void RefreshAll()
        {
            RefreshConfiguration();
            RefreshHistory();
            RefreshStream();
        }

        private void RefreshConfiguration()
        {
            if (_configStatus == null || _configDetails == null) return;
            
            _configDetails.Clear();
            
            if (_configurationAsset == null)
            {
                _configStatus.text = "No configuration asset found. Click to select or create.";
                _configStatus.style.color = new StyleColor(new Color(0.8f, 0.6f, 0.4f));
                
                var selectButton = new Button(() => SelectOrCreateConfiguration())
                {
                    text = "Select/Create Configuration Asset"
                };
                selectButton.AddToClassList("button-control");
                _configDetails.Add(selectButton);
                return;
            }
            
            // Add asset selection field
            var assetField = new ObjectField("Configuration Asset")
            {
                objectType = typeof(ContractsConfigurationAsset),
                value = _configurationAsset
            };
            
            assetField.RegisterValueChangedCallback(evt =>
            {
                _configurationAsset = evt.newValue as ContractsConfigurationAsset;
                RefreshConfiguration();
            });
            _configDetails.Add(assetField);
            
            // Add settings/providers data display
            if (_configurationAsset.settings != null)
            {
                var settingsFoldout = new Foldout { text = "Meta Module Settings", value = false };
                settingsFoldout.AddToClassList("config-settings-foldout");
                
                var settingsContainer = new VisualElement();
                settingsContainer.style.paddingLeft = 10;
                settingsContainer.style.paddingTop = 5;
                settingsContainer.style.paddingBottom = 5;
                
                var settings = _configurationAsset.settings;
                
                // Use Default Backend First
                var useDefaultField = new Toggle("Use Default Backend First") { value = settings.useDefaultBackendFirst };
                useDefaultField.RegisterValueChangedCallback(evt => 
                {
                    settings.useDefaultBackendFirst = evt.newValue;
                    EditorUtility.SetDirty(_configurationAsset);
                });
                settingsContainer.Add(useDefaultField);
                
                // History Size
                var historySizeField = new IntegerField("History Size") { value = settings.historySize };
                historySizeField.RegisterValueChangedCallback(evt => 
                {
                    settings.historySize = evt.newValue;
                    EditorUtility.SetDirty(_configurationAsset);
                });
                settingsContainer.Add(historySizeField);
                
                // Backend Type - Dropdown selector
                if (settings.backendTypes != null && settings.backendTypes.Count > 0)
                {
                    var backendNames = settings.backendTypes.Select(bt => bt.Name).ToList();
                    var currentBackendType = settings.backendTypes.FirstOrDefault(bt => bt.Id == settings.backendType);
                    var currentIndex = currentBackendType != null 
                        ? settings.backendTypes.IndexOf(currentBackendType) 
                        : 0;
                    
                    var backendTypeDropdown = new PopupField<string>("Active Backend Type", backendNames, currentIndex);
                    backendTypeDropdown.RegisterValueChangedCallback(evt =>
                    {
                        var selectedBackend = settings.backendTypes.FirstOrDefault(bt => bt.Name == evt.newValue);
                        if (selectedBackend != null)
                        {
                            settings.backendType.value = selectedBackend.Id;
                            EditorUtility.SetDirty(_configurationAsset);
                        }
                    });
                    settingsContainer.Add(backendTypeDropdown);
                }
                else
                {
                    var noBackendLabel = new Label("No backend types configured");
                    noBackendLabel.style.color = new StyleColor(new Color(0.8f, 0.6f, 0.4f));
                    settingsContainer.Add(noBackendLabel);
                }
                
                // Backend Types List
                if (settings.backendTypes != null && settings.backendTypes.Count > 0)
                {
                    var backendTypesFoldout = new Foldout { text = $"Backend Types ({settings.backendTypes.Count})", value = false };
                    backendTypesFoldout.style.marginTop = 5;
                    
                    foreach (var backendType in settings.backendTypes)
                    {
                        var backendItem = new VisualElement();
                        backendItem.style.paddingLeft = 10;
                        backendItem.style.paddingTop = 2;
                        backendItem.style.paddingBottom = 2;
                        backendItem.style.borderBottomWidth = 1;
                        backendItem.style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.5f));
                        
                        var nameLabel = new Label($"Name: {backendType.Name}");
                        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        backendItem.Add(nameLabel);
                        
                        var idLabel = new Label($"ID: {backendType.Id}");
                        idLabel.style.fontSize = 11;
                        backendItem.Add(idLabel);
                        
                        var providerField = new ObjectField("Provider")
                        {
                            objectType = typeof(BackendMetaServiceAsset),
                            value = backendType.Provider
                        };
                        providerField.RegisterValueChangedCallback(evt => 
                        {
                            backendType.Provider = evt.newValue as BackendMetaServiceAsset;
                            EditorUtility.SetDirty(_configurationAsset);
                        });
                        backendItem.Add(providerField);
                        
                        backendTypesFoldout.Add(backendItem);
                    }
                    
                    settingsContainer.Add(backendTypesFoldout);
                }
                
                settingsFoldout.Add(settingsContainer);
                _configDetails.Add(settingsFoldout);
            }
            
            // Display contracts
            var metaData = _configurationAsset.configuration?.remoteMetaData;
            
            // Diagnostic info
            if (_configurationAsset.configuration == null)
            {
                Debug.LogWarning($"ContractsConfigurationAsset.configuration is null for asset: {AssetDatabase.GetAssetPath(_configurationAsset)}");
            }
            else if (metaData == null)
            {
                Debug.LogWarning($"ContractsConfigurationAsset.configuration.remoteMetaData is null for asset: {AssetDatabase.GetAssetPath(_configurationAsset)}");
            }
            
            var contractCount = metaData?.Length ?? 0;
            
            // Add contracts foldout (always show, even if empty)
            var contractsFoldout = new Foldout { text = $"Contracts ({contractCount})", value = true };
            contractsFoldout.AddToClassList("config-contracts-foldout");
            contractsFoldout.style.marginTop = 10;
            
            var contractsContainer = new VisualElement();
            contractsContainer.style.paddingLeft = 5;
            
            if (contractCount == 0)
            {
                _configStatus.text = "Configuration loaded but no contracts found";
                _configStatus.style.color = new StyleColor(new Color(0.8f, 0.6f, 0.4f));
                
                var emptyLabel = new Label("No contracts configured. Use 'Update Remote Meta Data' button on the configuration asset.");
                emptyLabel.style.paddingLeft = 10;
                emptyLabel.style.paddingTop = 5;
                emptyLabel.style.paddingBottom = 5;
                emptyLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                emptyLabel.style.whiteSpace = WhiteSpace.Normal;
                contractsContainer.Add(emptyLabel);
            }
            else
            {
                _configStatus.text = $"Configuration loaded: {contractCount} contracts";
                _configStatus.style.color = new StyleColor(new Color(0.4f, 0.8f, 0.4f));
                
                // Display contracts
                var displayLimit = 100;
                foreach (var meta in metaData.Take(displayLimit))
                {
                    var item = CreateConfigItem(meta);
                    contractsContainer.Add(item);
                }
                
                if (contractCount > displayLimit)
                {
                    var moreLabel = new Label($"... and {contractCount - displayLimit} more contracts");
                    moreLabel.AddToClassList("status-label");
                    moreLabel.style.paddingLeft = 10;
                    moreLabel.style.paddingTop = 5;
                    moreLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                    contractsContainer.Add(moreLabel);
                }
            }
            
            contractsFoldout.Add(contractsContainer);
            _configDetails.Add(contractsFoldout);
        }
        
        private void SelectOrCreateConfiguration()
        {
            var path = EditorUtility.OpenFilePanel("Select Contracts Configuration Asset", "Assets", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                _configurationAsset = AssetDatabase.LoadAssetAtPath<ContractsConfigurationAsset>(path);
                RefreshConfiguration();
            }
        }

        private VisualElement CreateConfigItem(RemoteMetaData meta)
        {
            var container = new VisualElement();
            container.AddToClassList("config-item");
            container.AddToClassList(meta.enabled ? "config-item-enabled" : "config-item-disabled");
            
            var header = new VisualElement();
            header.AddToClassList("config-item-header");
            
            var methodLabel = new Label(meta.method ?? "Unknown");
            methodLabel.AddToClassList("config-item-method");
            header.Add(methodLabel);
            
            var idLabel = new Label($"ID: {meta.id}");
            idLabel.AddToClassList("config-item-id");
            header.Add(idLabel);
            
            container.Add(header);
            
            if (meta.contract != null)
            {
                var inputType = meta.contract.InputType?.Name ?? "Unknown";
                var outputType = meta.contract.OutputType?.Name ?? "Unknown";
                var typeLabel = new Label($"Input: {inputType} → Output: {outputType}");
                typeLabel.AddToClassList("config-item-type");
                container.Add(typeLabel);
            }
            
            if (meta.overrideProvider)
            {
                var providerLabel = new Label($"Provider: {meta.provider}");
                providerLabel.AddToClassList("config-item-provider");
                container.Add(providerLabel);
            }
            
            return container;
        }

        private void RefreshHistory()
        {
            if (_historyContainer == null) return;
            
            _historyContainer.Clear();
            
            if (_service == null)
            {
                var emptyState = CreateEmptyState("Service not available. Start play mode to see contract history.");
                _historyContainer.Add(emptyState);
                return;
            }
            
            var history = _service.ContractHistory;
            if (history == null || history.Length == 0)
            {
                var emptyState = CreateEmptyState("No contract history available yet.");
                _historyContainer.Add(emptyState);
                return;
            }
            
            // Filter and sort history
            var filteredHistory = history
                .Where(h => h != null && h.result != null)
                .Where(h => MatchesSearchFilter(h))
                .OrderByDescending(h => h.id)
                .Take(100);
            
            foreach (var item in filteredHistory)
            {
                var historyItem = CreateHistoryItem(item);
                _historyContainer.Add(historyItem);
            }
        }

        private bool MatchesSearchFilter(ContractHistoryItem item)
        {
            if (string.IsNullOrEmpty(_currentSearchFilter)) return true;
            
            var result = item.result;
            var filter = _currentSearchFilter.ToLower();
            
            if (result.metaId.ToString().Contains(filter)) return true;
            if (result.resultType?.Name?.ToLower().Contains(filter) ?? false) return true;
            if (result.error?.ToLower().Contains(filter) ?? false) return true;
            
            return false;
        }

        private VisualElement CreateHistoryItem(ContractHistoryItem historyItem)
        {
            var result = historyItem.result;
            
            var container = new VisualElement();
            container.AddToClassList("history-item");
            container.AddToClassList(result.success ? "history-item-success" : "history-item-error");
            
            // Header
            var header = new VisualElement();
            header.AddToClassList("history-item-header");
            
            var leftHeader = new VisualElement();
            leftHeader.style.flexDirection = FlexDirection.Row;
            leftHeader.style.alignItems = Align.Center;
            
            var idLabel = new Label($"#{historyItem.id}");
            idLabel.AddToClassList("history-item-id");
            leftHeader.Add(idLabel);
            
            var copyButton = new Button(() => CopyResultToClipboard(result))
            {
                text = "📋"
            };
            copyButton.tooltip = "Copy result to clipboard";
            copyButton.style.marginLeft = 5;
            copyButton.style.paddingLeft = 5;
            copyButton.style.paddingRight = 5;
            leftHeader.Add(copyButton);
            
            header.Add(leftHeader);
            
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(result.timestamp).LocalDateTime;
            var timeLabel = new Label(timestamp.ToString("HH:mm:ss"));
            timeLabel.AddToClassList("history-item-timestamp");
            header.Add(timeLabel);
            
            container.Add(header);
            
            // Contract ID
            var contractIdDisplay = !string.IsNullOrEmpty(result.contractId) 
                ? result.contractId 
                : result.metaId.ToString();
            var contractIdLabel = new Label($"Contract: {contractIdDisplay}");
            contractIdLabel.AddToClassList("history-item-meta-id");
            container.Add(contractIdLabel);
            
            // Status
            var statusContainer = new VisualElement();
            statusContainer.AddToClassList("history-item-status");
            
            var statusLabel = new Label(result.success ? "✓ Success" : "✗ Failed");
            statusLabel.AddToClassList(result.success ? "history-item-success-label" : "history-item-error-label");
            statusContainer.Add(statusLabel);
            
            container.Add(statusContainer);
            
            // Type
            if (result.resultType != null)
            {
                var typeLabel = new Label($"Type: {result.resultType.Name}");
                typeLabel.AddToClassList("history-item-type");
                container.Add(typeLabel);
            }
            
            // Result/Error
            if (!result.success && !string.IsNullOrEmpty(result.error))
            {
                var errorLabel = new Label($"Error: {result.error}");
                errorLabel.AddToClassList("history-item-error-message");
                container.Add(errorLabel);
            }
            else if (result.result != null)
            {
                var resultStr = FormatResult(result.result);
                if (!string.IsNullOrEmpty(resultStr))
                {
                    var resultLabel = new Label(resultStr);
                    resultLabel.AddToClassList("history-item-result");
                    container.Add(resultLabel);
                }
            }
            
            // Hash
            var hashLabel = new Label($"Hash: {result.hash}");
            hashLabel.AddToClassList("history-item-hash");
            container.Add(hashLabel);
            
            return container;
        }

        private void RefreshStream()
        {
            if (_streamContainer == null) return;
            
            _streamContainer.Clear();
            
            if (_streamResults.Count == 0)
            {
                var emptyState = CreateEmptyState("Waiting for contract executions...");
                _streamContainer.Add(emptyState);
                return;
            }
            
            foreach (var result in _streamResults)
            {
                var streamItem = CreateStreamItem(result);
                _streamContainer.Add(streamItem);
            }
            
            // Auto-scroll to top
            _streamScroll?.ScrollTo(_streamContainer.Children().FirstOrDefault());
        }

        private VisualElement CreateStreamItem(ContractDataResult result)
        {
            var container = new VisualElement();
            container.AddToClassList("stream-item");
            container.AddToClassList(result.success ? "stream-item-success" : "stream-item-error");
            
            // Header
            var header = new VisualElement();
            header.AddToClassList("stream-item-header");
            
            var leftHeader = new VisualElement();
            leftHeader.style.flexDirection = FlexDirection.Row;
            leftHeader.style.alignItems = Align.Center;
            
            var contractIdDisplay = !string.IsNullOrEmpty(result.contractId) 
                ? result.contractId 
                : result.metaId.ToString();
            var contractIdLabel = new Label($"Contract: {contractIdDisplay}");
            contractIdLabel.AddToClassList("stream-item-meta-id");
            leftHeader.Add(contractIdLabel);
            
            var copyButton = new Button(() => CopyResultToClipboard(result))
            {
                text = "📋"
            };
            copyButton.tooltip = "Copy result to clipboard";
            copyButton.style.marginLeft = 5;
            copyButton.style.paddingLeft = 5;
            copyButton.style.paddingRight = 5;
            leftHeader.Add(copyButton);
            
            header.Add(leftHeader);
            
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(result.timestamp).LocalDateTime;
            var timeLabel = new Label(timestamp.ToString("HH:mm:ss.fff"));
            timeLabel.AddToClassList("stream-item-time");
            header.Add(timeLabel);
            
            container.Add(header);
            
            // Status
            var statusLabel = new Label(result.success ? "✓ Success" : $"✗ Error: {result.error}");
            statusLabel.AddToClassList("stream-item-status");
            container.Add(statusLabel);
            
            // Result preview
            if (result.success && result.result != null)
            {
                var resultStr = FormatResult(result.result, maxLength: 200);
                if (!string.IsNullOrEmpty(resultStr))
                {
                    var resultLabel = new Label(resultStr);
                    resultLabel.AddToClassList("stream-item-result");
                    container.Add(resultLabel);
                }
            }
            
            return container;
        }

        private void UpdateStreamStatus(bool connected)
        {
            if (_streamStatus == null) return;
            
            if (connected)
            {
                _streamStatus.text = "⬤ Connected";
                _streamStatus.RemoveFromClassList("disconnected");
                _streamStatus.AddToClassList("connected");
            }
            else
            {
                _streamStatus.text = "⬤ Disconnected";
                _streamStatus.RemoveFromClassList("connected");
                _streamStatus.AddToClassList("disconnected");
            }
        }

        private void ClearHistory()
        {
            _streamResults.Clear();
            RefreshStream();
        }

        private void ExportHistory()
        {
            if (_service == null)
            {
                EditorUtility.DisplayDialog("Export Error", "No service available to export history.", "OK");
                return;
            }

            var history = _service.ContractHistory;
            if (history == null || history.Length == 0)
            {
                EditorUtility.DisplayDialog("Export Error", "No history data to export.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel("Export Contract History", 
                Application.dataPath, 
                $"contract_history_{System.DateTime.Now:yyyyMMdd_HHmmss}.json", 
                "json");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var validHistory = history.Where(h => h != null && h.result != null).ToList();
                var json = JsonConvert.SerializeObject(validHistory, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                System.IO.File.WriteAllText(path, json);
                EditorUtility.DisplayDialog("Export Success", 
                    $"Successfully exported {validHistory.Count} history items to:\n{path}", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Export Error", 
                    $"Failed to export history:\n{e.Message}", "OK");
                Debug.LogException(e);
            }
        }

        private VisualElement CreateEmptyState(string message)
        {
            var container = new VisualElement();
            container.AddToClassList("empty-state");
            
            var label = new Label(message);
            label.AddToClassList("empty-state-label");
            container.Add(label);
            
            return container;
        }

        private string FormatResult(object result, int maxLength = 500)
        {
            if (result == null) return string.Empty;
            
            try
            {
                if (result is string str)
                {
                    return str.Length > maxLength ? str.Substring(0, maxLength) + "..." : str;
                }
                
                var json = JsonConvert.SerializeObject(result, Formatting.Indented);
                return json.Length > maxLength ? json.Substring(0, maxLength) + "..." : json;
            }
            catch
            {
                var toString = result.ToString();
                return toString.Length > maxLength ? toString.Substring(0, maxLength) + "..." : toString;
            }
        }

        private void CopyResultToClipboard(ContractDataResult result)
        {
            if (result == null) return;
            
            try
            {
                var copyData = new System.Text.StringBuilder();
                copyData.AppendLine($"Meta ID: {result.metaId}");
                copyData.AppendLine($"Timestamp: {DateTimeOffset.FromUnixTimeSeconds(result.timestamp).LocalDateTime}");
                copyData.AppendLine($"Success: {result.success}");
                copyData.AppendLine($"Result Type: {result.resultType?.Name}");
                copyData.AppendLine($"Hash: {result.hash}");
                
                if (!result.success && !string.IsNullOrEmpty(result.error))
                {
                    copyData.AppendLine($"Error: {result.error}");
                }
                else if (result.result != null)
                {
                    copyData.AppendLine("Result:");
                    copyData.AppendLine(FormatResult(result.result, maxLength: int.MaxValue));
                }
                
                GUIUtility.systemCopyBuffer = copyData.ToString();
                Debug.Log("Contract result copied to clipboard");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to copy to clipboard: {e.Message}");
            }
        }
        
        private void CreateFallbackUI()
        {
            var label = new Label("Failed to load UI. Please ensure MetaBackendEditorWindow.uxml is in the correct location.");
            label.style.paddingTop = 20;
            label.style.paddingBottom = 20;
            label.style.paddingLeft = 20;
            label.style.paddingRight = 20;
            label.style.color = new StyleColor(Color.red);
            _root.Add(label);
        }
    }
}
