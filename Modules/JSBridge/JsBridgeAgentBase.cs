namespace Game.Modules.unity.meta.backend.Modules.JSBridge
{
    using System;
    using UnityEngine;

    public abstract class JsBridgeAgentBase : MonoBehaviour
    {
        public event Action<JsMetaMessageData> OnReceiveMessage;

        protected void InvokeReceiveMessage(string message)
        {
            OnReceiveMessage?.Invoke(new JsMetaMessageData
            { 
                Message = message
            });
        }
    }
}