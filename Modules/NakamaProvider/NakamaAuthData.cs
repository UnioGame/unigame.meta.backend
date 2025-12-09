namespace UniGame.MetaBackend.Runtime
{
    using System;

    [Serializable]
    public struct NakamaSessionData
    {
        public string UserId;
        public string Username;
        public string AuthToken;
        public string RefreshToken;
        public string AuthType;
    }
}