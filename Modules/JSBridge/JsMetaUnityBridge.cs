namespace Game.Modules.unity.meta.backend.Modules.JSBridge
{
    using System.Runtime.InteropServices;

    public static class JsMetaUnityBridge
    {
        [DllImport("__internal__")]
        public static extern object ReceiveMessageFromUnity(int messageId, object payload);
    }
}