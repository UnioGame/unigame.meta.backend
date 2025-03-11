namespace Game.Modules.unity.meta.service.Modules.NakamaServer
{
    using System;
    using UniGame.MetaBackend.Shared;

    [Serializable]
    public class NakamaRpcContract : IRemoteMetaContract
    {
        public object payload = string.Empty;
        public Type output;
        public Type input;
        public string method = string.Empty;
        
        public object Payload => payload;
        public string MethodName => method;
        public Type OutputType => output;
        public Type InputType => input;
    }
}