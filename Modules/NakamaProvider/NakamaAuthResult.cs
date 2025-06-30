namespace UniGame.MetaBackend.Runtime
{
    public struct NakamaAuthResult
    {
        public string token;
        public bool success;
        public string error;
        public NakamaConnection connection;
    }
}