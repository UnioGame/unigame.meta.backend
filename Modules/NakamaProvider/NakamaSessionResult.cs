namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Nakama;

    [Serializable]
    public struct NakamaSessionResult
    {
        public bool success;
        public string error;
        public ISession session;
    }
}