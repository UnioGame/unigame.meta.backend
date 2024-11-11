namespace Game.Modules.unity.meta.backend.Modules.JSBridge
{
    using System;
    using JetBrains.Annotations;
    using UnityEngine;

    public class JsMetaMonoAgent : MonoBehaviour
    {
        
        public event Action<JsMetaMessageData> OnReceiveMessage;

#if UNITY_EDITOR
        public string JsonMockConfig = "{\n  \"threshold\": 5,\n  \"view\": 0,\n  \"finish\": 1200,\n  \"speed\": 4,\n  \"acc\": { \"base\": 0, \"mods\": [{ \"ticks\": 800, \"value\": 0.25 }] },\n  \"obstacles\": {\n    \"cluster\": { \"base\": 3, \"mods\": [{ \"ticks\": 800, \"value\": 2 }] },\n    \"window\": { \"base\": 400, \"mods\": [{ \"ticks\": 800, \"value\": 425 }] },\n    \"gap\": { \"base\": 400, \"mods\": [{ \"ticks\": 800, \"value\": 475 }] },\n    \"distance\": { \"base\": 350, \"mods\": [] }\n  }\n}\n";
        private void Update()
        {
            if (Input.GetKey(KeyCode.G))
            {
                ReceiveMessage(0, JsonMockConfig);
            }
        }
        
#endif
        
        [UsedImplicitly]
        public void ReceiveMessage(int type, string message)
        {
            OnReceiveMessage?.Invoke(new JsMetaMessageData
            {
                Type = type,
                Message = message
            });
        }
    }
}