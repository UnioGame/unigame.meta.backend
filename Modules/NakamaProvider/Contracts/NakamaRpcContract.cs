namespace UniGame.MetaBackend.Runtime.Contracts
{
    using System;
    using Shared;

    [Serializable]
    public class NakamaRpcContract : RemoteMetaContract<string, string> ,INakamaContract
    {
        public string rpcName = string.Empty;
        public string payload = string.Empty;
        
        public override string Path => rpcName;
        
        public override object Payload => payload;
    }
}