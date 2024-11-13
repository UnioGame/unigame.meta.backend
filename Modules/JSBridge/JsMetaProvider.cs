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
    using UniRx;
    using Object = UnityEngine.Object;

    public class JsMetaProvider : IRemoteMetaProvider
    {
        private readonly JsMetaContractConfig _config;
        private readonly JsBridgeAgentBase _bridgePrefab;
        
        private JsBridgeAgentBase _metaMonoAgent;
        private Queue<JsMetaMessageData> _incomingRequestsQueue;
        
        public ILifeTime LifeTime { get; }

        private ReactiveProperty<ConnectionState> _state;
        public IReadOnlyReactiveProperty<ConnectionState> State => _state;

        public JsMetaProvider(JsMetaContractConfig config, JsBridgeAgentBase bridgeAgentBase)
        {
            _config = config;
            _state = new ReactiveProperty<ConnectionState>();
            _incomingRequestsQueue = new Queue<JsMetaMessageData>();
            _bridgePrefab = bridgeAgentBase;
        }
        
        public UniTask<MetaConnectionResult> ConnectAsync()
        {
            _metaMonoAgent = Object.Instantiate(_bridgePrefab);
            _metaMonoAgent.gameObject.name = "JsBridge_Agent";
            Object.DontDestroyOnLoad(_metaMonoAgent.gameObject);
            
            _metaMonoAgent.OnReceiveMessage += MessageReceivedCallback;

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
            _metaMonoAgent.OnReceiveMessage -= MessageReceivedCallback;
            Object.Destroy(_metaMonoAgent.gameObject);
            return UniTask.CompletedTask;
        }
        
        public bool IsContractSupported(IRemoteMetaContract command)
        {
            return IsContractSupported(command.MethodName, out var contractData);
        }

        public UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData contractData)
        {
            var contractConfig = _config.contracts
                .FirstOrDefault(x => x.contract.Name == contractData.contractName);

            var contractId = contractConfig.id;
            var payloadBytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(contractData.contract.Payload));
            var utf8StringPayload = Encoding.UTF8.GetString(payloadBytes);

            var result = new RemoteMetaResult
            {
                data = JsMetaUnityBridge.ReceiveMessageFromUnity(contractId, utf8StringPayload),
                error = null,
                success = true,
                id = contractData.contractName
            };

            return UniTask.FromResult(result);
        }

        public bool TryDequeue(out RemoteMetaResult result)
        {
            if (!(_incomingRequestsQueue.TryDequeue(out var request) && IsContractSupported(request, out var contract, out var message)))
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsContractSupported(string methodName, out JsMetaContractData contractData)
        {
            contractData = _config.contracts.FirstOrDefault(x => x.contract.Name == methodName);
            if (contractData == default)
            {
                GameLog.LogError($"No js meta contract config with method: {methodName}");
                return false;
            }

            return true;
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