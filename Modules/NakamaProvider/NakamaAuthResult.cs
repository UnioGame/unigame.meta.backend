namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Nakama;

    [Serializable]
    public class NakamaAuthResult
    {
        public bool created;
        public string userId;
        public IApiAccount account;
        public bool success;
        public string error;
    }
}
