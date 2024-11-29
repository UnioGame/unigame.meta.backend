namespace Game.Modules.unity.meta.backend.Modules.JSBridge
{
    using System;
    using Sirenix.OdinInspector;
    using UniCore.Runtime.ProfilerTools;
    using UniRx;
    using UnityEngine;

    public class JsBridgeAgentBase : MonoBehaviour,IDisposable, IJsBridgeAgent
    {
        #region inspector

        [Multiline]
        public string debugMessage = string.Empty;

        #endregion
        
        private Subject<JsMetaMessageData> _messageStream = new();
        
        public IObservable<JsMetaMessageData> MessageStream => _messageStream;
        
        public void InvokeReceiveMessage(string message)
        {
            GameLog.Log($"[JsBridgeAgentBase] SendMessageToGame: {message}");
            _messageStream.OnNext(new JsMetaMessageData
            { 
                Message = message
            });
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
        
        public void SendMessageToJs(string message)
        {
           
        }
    }
}