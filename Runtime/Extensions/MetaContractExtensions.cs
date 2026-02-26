namespace Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UnityEngine;

    public static class MetaContractExtensions
    {
        public const string MetaServiceNotInitializedError = "MetaService is not initialized";
        public static IBackendMetaService RemoteMetaService;
        public static Dictionary<string, ContractResultCache> ContractDataCache = new();
        public static Dictionary<string, DebounceMetaContract> DebounceContracts = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetData()
        {
            RemoteMetaService = null;
            ContractDataCache?.Clear();
            DebounceContracts?.Clear();
        }
        
        public static async UniTask<ContractMetaResult<TResult>> ExecuteAsync<TResult>(this IRemoteMetaContract contract, CancellationToken cancellationToken = default) 
            where TResult : class
        {
            if (RemoteMetaService == null)
            {
                return new ContractMetaResult<TResult>
                {
                    success = false,
                    error = MetaServiceNotInitializedError,
                };
            }
            
            var result = await contract.ExecuteAsync(cancellationToken);
            
            var resultValue = new ContractMetaResult<TResult>
            {
                success = result.success,
                error = result.error,
                data = result.model as TResult,
                statusCode = result.statusCode,
            };
            
            return resultValue;
        }

        public static async UniTask<ContractMetaResult<TResult>> ExecuteDebounceAsync<TResult>(
            this IRemoteMetaContract contract,
            CancellationToken cancellationToken = default) where TResult : class
        {
            return await contract.ExecuteDebounceAsync<TResult>(DebounceMetaContract.DefaultInterval, cancellationToken);
        }

        public static async UniTask<ContractMetaResult<TResult>> ExecuteDebounceAsync<TResult>(this IRemoteMetaContract contract,
            TimeSpan timeSpan,
            CancellationToken cancellationToken = default) where TResult : class
        {
            if (RemoteMetaService == null)
            {
                return new ContractMetaResult<TResult>
                {
                    success = false,
                    error = MetaServiceNotInitializedError,
                };
            }

            var contractId = contract.Path;
            if(!DebounceContracts.TryGetValue(contractId,out var debounceContract))
            {
                debounceContract = new DebounceMetaContract();
                DebounceContracts[contractId] = debounceContract;
            }
            
            debounceContract.interval = timeSpan;
            
            var result = await debounceContract.ExecuteAsync(contract, cancellationToken);
            
            var resultValue = new ContractMetaResult<TResult>
            {
                success = result.success,
                error = result.error,
                data = result.model as TResult,
                statusCode = result.statusCode,
            };
            
            return resultValue;
        }

        public static async UniTask<ContractMetaResult<TResult,TError>> ExecuteAsync<TResult,TError>(
            this IRemoteMetaContract contract,CancellationToken cancellationToken = default) 
            where TResult : class where TError : class
        {
            if (RemoteMetaService == null)
            {
                return new ContractMetaResult<TResult,TError>
                {
                    success = false,
                    error = MetaServiceNotInitializedError,
                };
            }
            
            var result = await contract.ExecuteAsync(cancellationToken);
            
            var resultValue = new ContractMetaResult<TResult,TError>
            {
                success = result.success,
                error = result.error,
                data = result.model as TResult,
                errorData = result.model as TError,
                statusCode = result.statusCode,
            };
            
            return resultValue;
        }

        public static async UniTask<ContractMetaResult<TOutput>> ExecuteAsync<TInput,TOutput>(
            this RemoteMetaContract<TInput,TOutput> contract,CancellationToken cancellationToken = default) 
            where TOutput : class
        {
            return await contract.ExecuteAsync<TOutput>(cancellationToken);
        }
        
        public static async UniTask<ContractMetaResult<TOutput>> ExecuteDebounceAsync<TInput,TOutput>(
            this RemoteMetaContract<TInput,TOutput> contract,
            CancellationToken cancellationToken = default) 
            where TOutput : class
        {
            return await contract.ExecuteDebounceAsync<TOutput>(cancellationToken);
        }
        
        public static async UniTask<ContractMetaResult<TOutput,TError>> ExecuteAsync<TInput,TOutput,TError>(
            this  RemoteMetaContract<TInput,TOutput,TError>  contract,
            CancellationToken cancellationToken = default) 
            where TOutput : class where TError : class
        {
            return await contract.ExecuteAsync<TOutput,TError>(cancellationToken);
        }

        public static async UniTask<ContractDataResult> ExecuteAsync(this IRemoteMetaContract contract, CancellationToken cancellationToken = default)
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

    public struct ContractResultCache
    {
        public string id;
        public int time;
        public int expireTime;
        public ContractDataResult result;
    }
}