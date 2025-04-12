namespace Extensions
{
    using System;
    using Cysharp.Threading.Tasks;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;

    public static class BackendMetaServiceExtensions
    {
        public static IBackendMetaService RemoteMetaService;

        public static async UniTask<MetaRequestResult<TResult>> ExecuteAsync<TResult>(this IRemoteMetaContract contract) 
            where TResult : class
        {
            if (RemoteMetaService == null)
            {
                return new MetaRequestResult<TResult>
                {
                    success = false,
                    error = "MetaService is not initialized",
                };
            }
            
            var result = await ExecuteAsync(contract);
            
            var resultValue = new MetaRequestResult<TResult>
            {
                success = result.success,
                error = result.error,
                data = result.model as TResult,
            };
            
            return resultValue;
        }

        public static async UniTask<MetaRequestResult<TResult,TError>> ExecuteAsync<TResult,TError>(this IRemoteMetaContract contract) 
            where TResult : class where TError : class
        {
            if (RemoteMetaService == null)
            {
                return new MetaRequestResult<TResult,TError>
                {
                    success = false,
                    error = "MetaService is not initialized",
                };
            }
            
            var result = await ExecuteAsync(contract);
            
            var resultValue = new MetaRequestResult<TResult,TError>
            {
                success = result.success,
                error = result.error,
                data = result.model as TResult,
                errorData = result.model as TError,
            };
            
            return resultValue;
        }

        public static async UniTask<MetaRequestResult<TOutput>> ExecuteAsync<TInput,TOutput>(
            this RemoteMetaContract<TInput,TOutput> contract) 
            where TOutput : class
        {
            return await contract.ExecuteAsync<TOutput>();
        }
        
        public static async UniTask<MetaRequestResult<TOutput,TError>> ExecuteAsync<TInput,TOutput,TError>(
            this  RemoteMetaContract<TInput,TOutput,TError>  contract) 
            where TOutput : class where TError : class
        {
            return await contract.ExecuteAsync<TOutput,TError>();
        }

        public static async UniTask<MetaDataResult> ExecuteAsync(this IRemoteMetaContract contract) 
        {
            if (RemoteMetaService == null)
            {
                return  new MetaDataResult
                {
                    success = false,
                    error = "MetaService is not initialized",
                };
            }
            
            var result = await RemoteMetaService.ExecuteAsync(contract);
            return result;
        }
        
    }
    
    [Serializable]
    public struct MetaRequestResult<TResult>
    {
        public TResult data;
        public bool success;
        public string error;
    }
    
    [Serializable]
    public struct MetaRequestResult<TResult,TError>
    {
        public TResult data;
        public TError errorData;
        public bool success;
        public string error;
    }
}