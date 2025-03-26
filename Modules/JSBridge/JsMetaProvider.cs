namespace Game.Modules.unity.meta.backend.Modules.JSBridge
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Cysharp.Threading.Tasks;
    using MetaService.Runtime;
    using Newtonsoft.Json;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UniModules.UniCore.Runtime.DataFlow;
    using UniRx;
    using Object = UnityEngine.Object;

    public class JsMetaProvider : IRemoteMetaJsProvider
    {
        public const string JsBridgeName = "JsBridge_Agent";
        
        private readonly JsMetaContractConfig _config;
        private readonly JsBridgeAgentBase _bridgePrefab;
        
        private JsBridgeAgentBase _metaAgent;
        private Queue<JsMetaMessageData> _incomingRequestsQueue;
        private LifeTime _lifeTime = new();
        private Dictionary<string, JsMetaContractData> _jsContracts = new(16);
        private ReactiveProperty<ConnectionState> _state;
        
        public ILifeTime LifeTime => _lifeTime;
        
        public IReadOnlyReactiveProperty<ConnectionState> State => _state;

        public JsMetaProvider(JsMetaContractConfig config, JsBridgeAgentBase bridgeAgentBase)
        {
            _lifeTime = new LifeTime();
            _config = config;
            _state = new ReactiveProperty<ConnectionState>()
                .AddTo(LifeTime);
            
            _incomingRequestsQueue = new Queue<JsMetaMessageData>();
            _bridgePrefab = bridgeAgentBase;
            _jsContracts = config.contracts.ToDictionary(x => x.contract.Name);
        }
        
        public UniTask<MetaConnectionResult> ConnectAsync()
        {
            if (_state.Value == ConnectionState.Connected)
            {
                return UniTask.FromResult(new MetaConnectionResult
                {
                    Success = true,
                    Error = string.Empty,
                    State = ConnectionState.Connected
                });    
            }
            
            _metaAgent = Object.Instantiate(_bridgePrefab);
            _metaAgent.gameObject.name = JsBridgeName;
            Object.DontDestroyOnLoad(_metaAgent.gameObject);
            
            _metaAgent.MessageStream
                .Subscribe(MessageReceivedCallback)
                .AddTo(LifeTime);

            _state.Value = ConnectionState.Connected;
            
            return UniTask.FromResult(new MetaConnectionResult
            {
                Success = true,
                Error = null,
                State = ConnectionState.Connected
            });
        }

        public UniTask DisconnectAsync()
        {
            _metaAgent.Dispose();
            Object.Destroy(_metaAgent.gameObject);
            return UniTask.CompletedTask;
        }
        
        public bool IsContractSupported(IRemoteMetaContract command)
        {
            var methodName = command.Path;
            if (_jsContracts.TryGetValue(methodName, out var contractData)) return true;
            GameLog.LogError($"No js meta contract config with method: {methodName}");
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsContractSupported(string command,out JsMetaContractData contractData)
        {
            var methodName = command;
            if (_jsContracts.TryGetValue(methodName, out contractData)) return true;
            GameLog.LogError($"No js meta contract config with method: {methodName}");
            return false;
        }

        public async UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData contractData)
        {
            if (!IsContractSupported(contractData.contractName, out var contractConfig))
            {
                return new RemoteMetaResult()
                {
                    success = false,
                    error = "contract not supported",
                    id = contractData.contractName,
                    data = null
                };
            }
            
            var contractId = contractConfig.id;
            var message = JsonConvert.SerializeObject(contractData.contract.Payload);
            var messageResult = _metaAgent.SendMessage(contractId, message);
            
            var result = new RemoteMetaResult
            {
                data = messageResult,
                error = null,
                success = true,
                id = contractData.contractName
            };

            return result;
        }

        public bool TryDequeue(out RemoteMetaResult result)
        {
            if (!(_incomingRequestsQueue.TryDequeue(out var request) && 
                  IsContractSupported(request, out var contract, out var message)))
            {
                result = default;
                return false;
            }
            
            result.id = contract.contract.Name;
            result.data = message;
            result.success = true;
            result.error = null;

            return true;
        }


        public void Dispose()
        {
            _lifeTime.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsContractSupported(JsMetaMessageData data, out JsMetaContractData contractData, out string innerMessage)
        {
            try
            {
                var uniMessage = JsonConvert.DeserializeObject<UniversalJsMessage>(data.Message);
                innerMessage = uniMessage.Content;
                contractData = _config.contracts.FirstOrDefault(x => x.id == uniMessage.MessageId);
                
                return contractData != default;
            }
            catch (Exception)
            {
                innerMessage = string.Empty;
                contractData = default;
                return false;
            }
        }

        private void MessageReceivedCallback(JsMetaMessageData data)
        {
            _incomingRequestsQueue.Enqueue(data);
        }
    }
}