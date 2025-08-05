namespace UniGame.MetaBackend.Runtime
{
    using System;

    [Serializable]
    public class NakamaServerData
    {
        public NakamaEndpoint endpoint;
        public string url;
        public string healthCheckUrl;
    }
}