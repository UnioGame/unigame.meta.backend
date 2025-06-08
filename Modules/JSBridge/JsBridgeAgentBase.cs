namespace Game.Modules.unity.meta.backend.Modules.JSBridge
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;
    using R3;
    using Sirenix.OdinInspector;
    using UniCore.Runtime.ProfilerTools;
     
    using UnityEngine;

    public class JsBridgeAgentBase : MonoBehaviour,IDisposable, IJsBridgeAgent
    {
        #region inspector

        [Multiline]
        public string debugMessage = string.Empty;

        #endregion
        
        private Subject<JsMetaMessageData> _messageStream = new();
        
        public Observable<JsMetaMessageData> MessageStream => _messageStream;
        
        public void InvokeReceiveMessage(string message)
        {
            GameLog.Log($"[JsBridgeAgentBase] SendMessageToGame: {message}");
            _messageStream.OnNext(new JsMetaMessageData
            { 
                Message = message
            });
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object SendMessage(int contractId, string message)
        {
            var payloadBytes = Encoding.Default.GetBytes(message);
            var utf8StringPayload = Encoding.UTF8.GetString(payloadBytes);
            var callJsBridge = true;
#if UNITY_EDITOR
            callJsBridge = false;
#endif
            var result = callJsBridge 
                ? string.Empty
                : JsMetaUnityBridge.ReceiveMessageFromUnity(contractId, utf8StringPayload);
            
            return result;
        }
        
        public void Dispose()
        {
            _messageStream.Dispose();
        }

        [Button]
        public void SendMessageToGame()
        {
            SendMessageToGame(debugMessage);
        }
        
        public void SendMessageToGame(string message)
        {
            InvokeReceiveMessage(message);
        }
    }
}