namespace UniGame.MetaBackend.Runtime
{
    using System;

    [Serializable]
    public struct ContractMetaResult
    {
        public static readonly ContractMetaResult FailedResult = new()
        {
            id = nameof(ContractMetaResult),
            data = null,
            success = false,
            error = "Request failed",
        };
        
        public string id;
        public object data;
        public bool success;
        public string error;
        public int statusCode;
    }
    
    [Serializable]
    public struct ContractMetaResult<TResult>
    {
        public string id;
        public TResult data;
        public bool success;
        public string error;
        public int statusCode;
    }
}