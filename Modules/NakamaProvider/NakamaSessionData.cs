namespace UniGame.MetaBackend.Runtime
{
    using System;

    [Serializable]
    public class NakamaSessionData
    {
        public string authType;
        public long timestamp;
        public string authToken;
        public string refreshToken;
    }
}