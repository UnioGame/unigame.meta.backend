namespace UniGame.MetaBackend.Runtime
{
    using System;

    [Serializable]
    public struct NakamaServiceResult
    {
        public bool success;
        public string error;
        public int statusCode;
    }
    
    public static class NakamaStatusCodes
    {
        public const int Success = 0;
        
        public const int NetworkError = 1000;
        public const int InvalidSession = 1001;
        public const int NotFound = 1002;
        public const int AlreadyExists = 1003;
        public const int PermissionDenied = 1004;
        public const int RateLimited = 1005;
        public const int UserWithThisNameAlreadyExists = 1006;
        
        public const int UnknownError = 2000;
    }
}