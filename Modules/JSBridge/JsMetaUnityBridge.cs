namespace UniGame.MetaBackend.Runtime
{
    using System.Runtime.InteropServices;

    public static class JsMetaUnityBridge
    {
        [DllImport("__internal__")]
        public static extern object ReceiveMessageFromUnity(int messageId, object payload);
    }
}