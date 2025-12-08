namespace Extensions
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;

    public static class BackendMetaServiceExtensions
    {
        public const string MetaServiceNotInitializedError = "MetaService is not initialized";
        public static IBackendMetaService RemoteMetaService;

        public static async UniTask<MetaRequestResult<TResult>> 
            ExecuteAsync<TResult>(this IRemoteMetaContract contract, CancellationToken cancellationToken = default) 
            where TResult : class
        {
            if (RemoteMetaService == null)
            {
                return new MetaRequestResult<TResult>
                {
                    success = false,
                    error = MetaServiceNotInitializedError,
                };
            }
            
            var result = await ExecuteAsync(contract,cancellationToken);
            
            var resultValue = new MetaRequestResult<TResult>
            {
                success = result.success,
                error = result.error,
                data = result.model as TResult,
                statusCode = result.statusCode,
            };
            
            return resultValue;
        }

        public static async UniTask<MetaRequestResult<TResult,TError>> 
            ExecuteAsync<TResult,TError>(this IRemoteMetaContract contract,CancellationToken cancellationToken = default) 
            where TResult : class where TError : class
        {
            if (RemoteMetaService == null)
            {
                return new MetaRequestResult<TResult,TError>
                {
                    success = false,
                    error = MetaServiceNotInitializedError,
                };
            }
            
            var result = await ExecuteAsync(contract,cancellationToken);
            
            var resultValue = new MetaRequestResult<TResult,TError>
            {
                success = result.success,
                error = result.error,
                data = result.model as TResult,
                errorData = result.model as TError,
                statusCode = result.statusCode,
            };
            
            return resultValue;
        }

        public static async UniTask<MetaRequestResult<TOutput>> ExecuteAsync<TInput,TOutput>(
            this RemoteMetaContract<TInput,TOutput> contract,CancellationToken cancellationToken = default) 
            where TOutput : class
        {
            return await contract.ExecuteAsync<TOutput>(cancellationToken);
        }
        
        public static async UniTask<MetaRequestResult<TOutput,TError>> ExecuteAsync<TInput,TOutput,TError>(
            this  RemoteMetaContract<TInput,TOutput,TError>  contract,
            CancellationToken cancellationToken = default) 
            where TOutput : class where TError : class
        {
            return await contract.ExecuteAsync<TOutput,TError>(cancellationToken);
        }

        public static async UniTask<ContractDataResult> ExecuteAsync(this IRemoteMetaContract contract, 
            CancellationToken cancellationToken = default)
        {
            if (RemoteMetaService == null)
            {
                return  new ContractDataResult
                {
                    success = false,
                    error = "MetaService is not initialized",
                };
            }
            
            var result = await RemoteMetaService.ExecuteAsync(contract, cancellationToken);
            return result;
        }
        
    }
    
    [Serializable]
    public struct MetaRequestResult<TResult>
    {
        public static readonly MetaRequestResult<TResult> FailedResult = new()
        {
            success = false,
            error = "Request failed",
            data = default
        };
        
        public TResult data;
        public bool success;
        public string error;
        public int statusCode;
    }
    
    [Serializable]
    public struct MetaRequestResult<TResult,TError>
    {
        public TResult data;
        public TError errorData;
        public bool success;
        public string error;
        public int statusCode;
    }
}