namespace MetaService.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using Game.Modules.ModelMapping;
    using R3;
    using Shared;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UniGame.GameFlow.Runtime;
    using UniGame.Runtime.DateTime;
    using UniGame.Runtime.Rx;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    public class BackendMetaService : GameService, IBackendMetaService
    {
        public const int ContractInitTimeout = 20;
        
#if UNITY_EDITOR
        public static BackendMetaService EditorInstance;
#endif

        private bool _useDefaultProvider;
        private bool _isInitialized;
        private ContractsProvidersData _settings;
        private IContext _context;
        private IRemoteMetaDataConfiguration _metaDataConfiguration;
        private IRemoteMetaProvider _defaultMetaProvider;
        private BackendTypeId _defaultProviderId;
        private IDictionary<int, IRemoteMetaProvider> _metaProviders;
        private Dictionary<int,ContractDataResult> _responceCache;
        private Dictionary<int,RemoteMetaData> _metaIdCache;
        private Dictionary<Type, IRemoteMetaProvider> _contractsCache;
        private Subject<ContractDataResult> _dataStream;
        private ReactiveValue<BackendMetaServiceState> _initializationState;
        private List<IMetaContractHandler> _contractHandlers = new();
        private IRemoteDataConverter _defaultConverter;
        private string _initializationError;
        private int _historySize;
        private int _historyIndex;
        private ContractHistoryItem[] _history;

        public BackendMetaService(ContractsProvidersData settings,IContext context, IRemoteMetaDataConfiguration metaDataConfiguration)
        {
            _isInitialized = false;
            _settings = settings;
            _context = context;
            _metaDataConfiguration = metaDataConfiguration;
            _useDefaultProvider = _settings.useDefaultBackendFirst;
            _defaultProviderId = _settings.backendType;
            
            MetaContractExtensions.RemoteMetaService = this;

            _defaultConverter = metaDataConfiguration.Converter ?? new JsonRemoteDataConverter();
            _initializationState = new ReactiveValue<BackendMetaServiceState>(BackendMetaServiceState.Initializing);
            _initializationError = string.Empty;
            _responceCache = new Dictionary<int, ContractDataResult>(64);
            _metaIdCache = new Dictionary<int, RemoteMetaData>(64);
            _contractsCache = new Dictionary<Type, IRemoteMetaProvider>(64);
            _dataStream = new Subject<ContractDataResult>().AddTo(LifeTime);
            _historySize = _settings.historySize;
            _history = new ContractHistoryItem[_historySize];
            _dataStream.Subscribe(AddHistoryItem).AddTo(LifeTime);

            UpdateMetaCache();
            InitializeProvidersAsync().Forget();
#if UNITY_EDITOR
            EditorInstance = this;
#endif
        }

        public struct RemoteMetaProviderResult
        {
            public int id;
            public bool success;
            public IRemoteMetaProvider provider;
        }
        
        public ContractHistoryItem[] ContractHistory => _history;
        
        public Observable<ContractDataResult> DataStream => _dataStream;

        public ReadOnlyReactiveProperty<BackendMetaServiceState> InitializationState => _initializationState;

        public string InitializationError => _initializationError;

        public bool AddContractHandler(IMetaContractHandler handler)
        {
            if (_contractHandlers.Contains(handler)) return false;
            _contractHandlers.Add(handler);
            return true;
        }

        public bool RemoveContractHandler<T>() where T : IMetaContractHandler
        {
            var count = _contractHandlers.RemoveAll(x => x is T);
            return count > 0;
        }

        public IRemoteMetaDataConfiguration MetaDataConfiguration => _metaDataConfiguration;
        
        public IRemoteMetaProvider FindProvider(MetaContractData data)
        {
            return ResolveProvider(data.metaData, data.contract);
        }     
        
        public IRemoteMetaProvider GetProvider(int providerId)
        {
            return _metaProviders != null && _metaProviders.TryGetValue(providerId, out var provider) 
                ? provider : _defaultMetaProvider;
        }        
        
        public bool RegisterProvider(int providerId,IRemoteMetaProvider provider)
        {
            _metaProviders ??= new Dictionary<int, IRemoteMetaProvider>();
            _metaProviders[providerId] = provider;
            _contractsCache.Clear();
            return true;
        }
        
        public void SwitchProvider(int providerId)
        {
            _defaultProviderId = (BackendTypeId)providerId;
            _defaultMetaProvider = GetProvider(providerId);
            _contractsCache.Clear();
        }

        public IRemoteMetaProvider SelectProvider(RemoteMetaData meta,IRemoteMetaContract contract)
        {
            return ResolveProvider(meta, contract);
        }

        public async UniTask<ContractDataResult> ExecuteAsync(IRemoteMetaContract contract,CancellationToken cancellation = default)
        {
            await WaitForInitializationAsync(cancellation);

            var initializationFailure = CreateInitializationFailureResult(contract);
            if (initializationFailure != null)
                return initializationFailure;
            
            var meta = FindMetaData(contract);
            var contractName = NormalizeContractName(contract, meta);
            var provider = SelectProvider(meta,contract);
            if (provider == null)
                return CreateFailureResult(contract, meta, contractName,
                    BackendMetaConstants.ProviderResolutionFailedError,
                    BackendMetaConstants.ProviderResolutionFailedStatusCode);
            
            var contractData = new MetaContractData()
            {
                id = meta.id,
                metaData = meta,
                contract = contract,
                provider = provider,
                contractName = contractName,
            };
            
            return await ExecuteAsync(contractData, cancellation).AttachExternalCancellation(LifeTime.Token);
        }

        public async UniTask<ContractDataResult> ExecuteAsync(
            MetaContractData contractData,
            CancellationToken cancellation = default)
        {
            await WaitForInitializationAsync(cancellation);
            
            try
            {
                var initializationFailure = CreateInitializationFailureResult(contractData.contract);
                if (initializationFailure != null)
                    return initializationFailure;

                var meta = contractData.metaData ?? RemoteMetaData.Empty;
                contractData.metaData = meta;
                var provider = contractData.provider ?? FindProvider(contractData);
                var contract = contractData.contract;
                var contractName = NormalizeContractName(contract, meta, contractData.contractName);

                if (provider == null)
                    return CreateFailureResult(contract, meta, contractName,
                        BackendMetaConstants.ProviderResolutionFailedError,
                        BackendMetaConstants.ProviderResolutionFailedStatusCode);

                contractData.provider = provider;
                contractData.contractName = contractName;
                
                var connectionResult = await ConnectAsync(provider,cancellation);
                if(connectionResult.Success == false) 
                    return CreateFailureResult(contract, meta, contractName,
                        string.IsNullOrEmpty(connectionResult.Error)
                            ? BackendMetaConstants.ProviderConnectionFailedError
                            : connectionResult.Error,
                        BackendMetaConstants.ProviderConnectionFailedStatusCode);
                
                if(!provider.IsContractSupported(contract))
                    return CreateFailureResult(contract, meta, contractName,
                        BackendMetaConstants.UnsupportedContract.error,
                        BackendMetaConstants.UnsupportedContract.statusCode);

                var contractValue = contractData.contract;
                foreach (var contractHandler in _contractHandlers)
                    contractValue = contractHandler.UpdateContract(contractValue); 
                
                contractData.contract = contractValue;
                
                var remoteResult = await provider.ExecuteAsync(contractData,cancellation);

                await UniTask.SwitchToMainThread();
                
                var result = ProcessRemoteResponse(contractData,remoteResult);

                _responceCache.TryGetValue(result.metaId, out var response);
                _responceCache[result.metaId] = result;
                
                var isChanged = response == null || response.hash != result.hash;
                if(isChanged && result.success) 
                    _dataStream.OnNext(result);
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return ContractDataResult.Empty;
            }
        }
        
        public RemoteMetaData FindMetaData(IRemoteMetaContract contract)
        {
            foreach (var meta in _metaDataConfiguration.RemoteMetaData)
            {
#if UNITY_EDITOR
                if (meta.contract == null)
                {
                    Debug.LogError($"Backend Service: {meta.id} {meta.method} {meta.provider}");
                    return RemoteMetaData.Empty;
                }
#endif
                
                if(meta.contract.GetType() == contract.GetType())
                    return meta;
            }
            
            return RemoteMetaData.Empty;
        }
        
        public RemoteMetaData FindMetaData(int metaId)
        {
            if (_metaIdCache.TryGetValue(metaId, out var metaData))
                return metaData;
            return RemoteMetaData.Empty;
        }
        

        public ContractDataResult ProcessRemoteResponse(MetaContractData contractData, ContractMetaResult response)
        {
            var remoteId = contractData.contractName;
            var contract = contractData.contract;
            var metaData = contractData.metaData;
            var success = response.success;
            
            var responseData = response.data ?? string.Empty;
            var unixTime = DateTime.Now.ToUnixTimestamp();
            var outputType = contract.OutputType;
            
            outputType = outputType == null || outputType == typeof(VoidRemoteData) 
                ? typeof(string)
                : outputType;
            
            var resultType = response.data !=null && success
                ? response.data.GetType()
                : outputType;
            
            var resultObject = responseData;
            
            switch (outputType)
            {
                case not null when outputType == typeof(string):
                    resultObject = responseData;
                    break;
                case not null when outputType != typeof(string) && resultType == typeof(string):
                    if (responseData is string responseString &&
                        !string.IsNullOrEmpty(responseString))
                    {
                        resultObject = _defaultConverter.Convert(outputType, responseString);
                    }
                    break;
                case not null when outputType == typeof(VoidRemoteData):
                    resultObject = VoidRemoteData.Empty;
                    break;
            }
            
            var result = new ContractDataResult()
            {
                contractId = remoteId,
                metaId = metaData.id,
                payload = contract?.Payload,
                resultType = outputType,
                model = resultObject,
                result = responseData,
                success = response.success,
                hash = responseData.GetHashCode(),
                error = response.error,
                statusCode = response.statusCode,
                timestamp = unixTime,
            };

            
#if GAME_DEBUG
            if (_settings.enableLogging)
            {
                var color = result.success ? Color.green : Color.red;
                GameLog.Log($"[{remoteId}]:  contract {contract?.GetType().Name} input {contract.InputType.Name} output {contract.OutputType.Name} " +
                            $"method: {contract.Path} | payload: {result.payload} | success: {result.success} | error: {result.error} | " +
                            $"result {result.result} {result.result?.GetType().Name} code {result.statusCode} \nresponse: \n{responseData}",color);
            }
#endif
            
            return result;
        }
        
        private async UniTask InitializeProvidersAsync()
        {
            _isInitialized = false;
            _initializationError = string.Empty;
            _initializationState.Value = BackendMetaServiceState.Initializing;
            _metaProviders = new Dictionary<int, IRemoteMetaProvider>();
            _contractsCache.Clear();
            _defaultMetaProvider = null;
            _defaultProviderId = _settings.backendType;

            try
            {
                var providersData = _settings.backendTypes ?? new List<BackendType>();

                foreach (var providerData in providersData)
                    providerData?.Normalize();

                var providerTasks = providersData.Select(x => CreateProvider(x, _context));
                var providers = await UniTask.WhenAll(providerTasks);

                foreach (var providerResult in providers)
                {
                    if(providerResult.success == false)
                        continue;

                    var id = providerResult.id;
                    var provider  = providerResult.provider;
                
                    _metaProviders[id] =provider;
                
                    if (id == _defaultProviderId)
                        _defaultMetaProvider =  provider;
                }
            
                if (_metaProviders.Count == 0)
                {
                    _initializationError = BackendMetaConstants.NoProvidersAvailableError;
                    _initializationState.Value = BackendMetaServiceState.Failed;
                    return;
                }

                if (_defaultMetaProvider == null)
                {
                    _initializationError = BackendMetaConstants.DefaultProviderMissingError;
                    _initializationState.Value = BackendMetaServiceState.Failed;
                    return;
                }

                _context.Publish<IRemoteMetaProvider>(_defaultMetaProvider);
            
                _isInitialized = true;
                _initializationState.Value = BackendMetaServiceState.Ready;
            }
            catch (Exception exception)
            {
                _initializationError = exception.Message;
                _initializationState.Value = BackendMetaServiceState.Failed;
                GameLog.LogException(exception);
            }
        }

        
        private async UniTask<RemoteMetaProviderResult> CreateProvider(BackendType providerData, IContext context)
        {
            var result = new RemoteMetaProviderResult()
            {
                id = providerData?.id ?? 0,
                success = false,
                provider = null,
            };
            
            if (providerData == null || !providerData.isEnabled) return result;

            providerData.Normalize();

            if (providerData.provider == null)
            {
                GameLog.LogError($"GameBackendSource: skip backend provider '{providerData.name ?? providerData.id.ToString()}' because provider asset is missing");
                return result;
            }

            var provider = providerData.provider;
            var providerSource = Object.Instantiate(provider);

            var metaProviderResponse = await providerSource.CreateAsync(context)
                .Timeout(TimeSpan.FromSeconds(ContractInitTimeout))
                .SuppressCancellationThrow();

            var metaProvider = metaProviderResponse.Result;

            if (metaProviderResponse.IsCanceled)
            {
                GameLog.LogError($"GameBackendSource: Register MetaProvider - {providerSource.name} TIMEOUT");
                return result;
            }

            metaProvider.AddTo(LifeTime);
            
            result.success = true;
            result.provider = metaProvider;

            return result;
        }
        
        private async UniTask<MetaConnectionResult> ConnectAsync(IRemoteMetaProvider provider, CancellationToken cancellation = default)
        {
            if (provider.State.CurrentValue != ConnectionState.Connected)
            {
                var connectionResult = await provider.ConnectAsync()
                    .AttachExternalCancellation(cancellation);
                return connectionResult;
            }

            return new MetaConnectionResult()
            {
                Success = true,
                Error = string.Empty,
                State = ConnectionState.Connected,
            };
        }

        private void UpdateMetaCache()
        {
            _metaIdCache.Clear();
            
            var items = _metaDataConfiguration.RemoteMetaData;
            foreach (var metaData in items)
            {
                if(_metaIdCache.TryGetValue((RemoteMetaId)metaData.id,out var _))
                    continue;
                _metaIdCache[(RemoteMetaId)metaData.id] = metaData;
            }
        }
        
        private void AddHistoryItem(ContractDataResult result)
        {
            var id = _historyIndex;
            _historyIndex++;
            
            var historyItem = new ContractHistoryItem()
            {
                id =id,
                result = result,
            };
            
            var index = id % _historySize;
            _history[index] = historyItem;
        }

        private async UniTask WaitForInitializationAsync(CancellationToken cancellation)
        {
            if (_initializationState.CurrentValue == BackendMetaServiceState.Initializing)
            {
                await UniTask.WaitWhile(this,
                    static service => service._initializationState.CurrentValue == BackendMetaServiceState.Initializing,
                    cancellationToken: cancellation);
            }
        }

        private ContractDataResult CreateInitializationFailureResult(IRemoteMetaContract contract)
        {
            if (_initializationState.CurrentValue != BackendMetaServiceState.Failed)
                return null;

            var contractName = NormalizeContractName(contract, RemoteMetaData.Empty);
            var error = string.IsNullOrEmpty(_initializationError)
                ? BackendMetaConstants.InitializationFailedError
                : $"{BackendMetaConstants.InitializationFailedError}: {_initializationError}";

            return CreateFailureResult(contract, RemoteMetaData.Empty, contractName, error,
                BackendMetaConstants.InitializationFailedStatusCode);
        }

        private ContractDataResult CreateFailureResult(
            IRemoteMetaContract contract,
            RemoteMetaData metaData,
            string contractName,
            string error,
            int statusCode)
        {
            return BackendMetaConstants.CreateFailureResult(contract, metaData, contractName, error, statusCode,
                DateTime.Now.ToUnixTimestamp());
        }

        private IRemoteMetaProvider ResolveProvider(RemoteMetaData meta, IRemoteMetaContract contract)
        {
            if (contract == null)
                return null;

            var contractType = contract.GetType();
            if (_contractsCache.TryGetValue(contractType, out var cachedProvider) &&
                cachedProvider != null &&
                cachedProvider.IsContractSupported(contract))
            {
                return cachedProvider;
            }

            var provider = ResolveProviderCore(meta, contract);
            if (provider != null)
                _contractsCache[contractType] = provider;

            return provider;
        }

        private IRemoteMetaProvider ResolveProviderCore(RemoteMetaData meta, IRemoteMetaContract contract)
        {
            if (meta != null && meta.overrideProvider)
            {
                return TryGetRegisteredProvider(meta.provider, out var overrideProvider) &&
                       overrideProvider.IsContractSupported(contract)
                    ? overrideProvider
                    : null;
            }

            var defaultProvider = GetDefaultProvider();
            if (_useDefaultProvider && defaultProvider != null && defaultProvider.IsContractSupported(contract))
                return defaultProvider;

            if (_metaProviders != null)
            {
                foreach (var registeredProvider in _metaProviders.Values)
                {
                    if (registeredProvider == null || !registeredProvider.IsContractSupported(contract))
                        continue;

                    return registeredProvider;
                }
            }

            return !_useDefaultProvider && defaultProvider != null && defaultProvider.IsContractSupported(contract)
                ? defaultProvider
                : null;
        }

        private IRemoteMetaProvider GetDefaultProvider()
        {
            return TryGetRegisteredProvider(_defaultProviderId, out var registeredProvider)
                ? registeredProvider
                : _defaultMetaProvider;
        }

        private bool TryGetRegisteredProvider(int providerId, out IRemoteMetaProvider provider)
        {
            provider = null;
            return _metaProviders != null && _metaProviders.TryGetValue(providerId, out provider) && provider != null;
        }

        private static string NormalizeContractName(
            IRemoteMetaContract contract,
            RemoteMetaData metaData,
            string contractName = null)
        {
            if (!string.IsNullOrEmpty(contractName))
                return contractName;

            if (!string.IsNullOrEmpty(contract?.Path))
                return contract.Path;

            if (metaData != null && !string.IsNullOrEmpty(metaData.method))
                return metaData.method;

            return string.Empty;
        }

        private RemoteMetaData CreateNewRemoteMeta(string methodName)
        {
            var contract = new SimpleMetaContract<string, string>();
            var id = _metaDataConfiguration.CalculateMetaId(contract);
            
            return new RemoteMetaData()
            {
                id = id,
                method = methodName,
                contract = contract,
            };
        }

    }
}