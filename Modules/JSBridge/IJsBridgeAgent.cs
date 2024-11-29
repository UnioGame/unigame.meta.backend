namespace Game.Modules.unity.meta.backend.Modules.JSBridge
{
    using System;

    public interface IJsBridgeAgent
    {
        IObservable<JsMetaMessageData> MessageStream { get; }
        
        void Dispose();
        void InvokeReceiveMessage(string message);
        object SendMessage(int contractId, string message);
    }
}