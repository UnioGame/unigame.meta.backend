namespace UniGame.MetaBackend.Runtime
{
    using System;
    using R3;

    public interface IJsBridgeAgent
    {
        Observable<JsMetaMessageData> MessageStream { get; }
        
        void Dispose();
        void InvokeReceiveMessage(string message);
        object SendMessage(int contractId, string message);
    }
}