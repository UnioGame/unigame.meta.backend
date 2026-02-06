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
            error = "request failed",
            statusCode = -1,
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
        public static readonly ContractMetaResult<TResult> FailedResult = new()
        {
            id = nameof(ContractMetaResult),
            data = default,
            success = false,
            error = "Request failed",
            statusCode = -1,
        };
        
        public string id;
        public TResult data;
        public bool success;
        public string error;
        public int statusCode;
    }
    
    [Serializable]
    public struct ContractMetaResult<TResult,TError>
    {
        public static readonly ContractMetaResult<TResult,TError> FailedResult = new()
        {
            id = nameof(ContractMetaResult),
            data = default,
            success = false,
            error = "Request failed",
            statusCode = -1,
            errorData =  default,
        };
        
        public string id;
        public TResult data;
        public TError errorData;
        public bool success;
        public string error;
        public int statusCode;
    }
}