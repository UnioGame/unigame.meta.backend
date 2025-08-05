namespace UniGame.MetaBackend.Runtime
{
    using System;

    [Serializable]
    public struct NakamaConnectionResult
    {
        public string userId;
        public bool success;
        public string error;
    }
}