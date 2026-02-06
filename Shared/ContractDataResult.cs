namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Runtime.CompilerServices;

    [Serializable]
    public class ContractDataResult
    {
        public static readonly ContractDataResult Empty = new()
        {
            hash = -1,
            resultType = typeof(string),
        };
        
        public string contractId = string.Empty;
        public int metaId = -1;
        public long timestamp = 0;
        public int hash = 0;
        public Type resultType;
        public object payload = string.Empty;
        public object result = string.Empty;
        public object model = null;
        public bool success = false;
        public string error = string.Empty;
        public int statusCode = 0;
    }


    public static class ContractDataResultExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContractMetaResult ToContractResult<T>(this ContractMetaResult<T> connectionResult)
        {
            return new ContractMetaResult
            {
                id = connectionResult.id,
                success = connectionResult.success,
                error = connectionResult.error,
                statusCode = connectionResult.statusCode,
                data = connectionResult.data,
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContractMetaResult<T> ToContractResult<T>(this ContractMetaResult connectionResult) where T : class
        {
            return new ContractMetaResult<T>
            {
                id = connectionResult.id,
                success = connectionResult.success,
                error = connectionResult.error,
                statusCode = connectionResult.statusCode,
                data = connectionResult.data as T,
            };
        }
    }
}