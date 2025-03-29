namespace Extensions
{
    using System;
    using Cysharp.Threading.Tasks;
    using UniGame.MetaBackend.Shared;
    using UnityEngine.Serialization;

#if UNITY_EDITOR
    using UnityEditor;
#endif
    
    public static class BackendMetaServiceExtensions
    {
        public static IBackendMetaService RemoteMetaService;

        public static async UniTask<MetaRequestResult<TOutput>> ExecuteAsync<TInput,TOutput>(
            this IRemoteMetaContract<TInput,TOutput> contract)
            where TOutput : class
        {
            return await ExecuteAsync<TOutput>(contract);
        }

        public static async UniTask<MetaRequestResult<TResult>> ExecuteAsync<TResult>(this IRemoteMetaContract contract) 
            where TResult : class
        {
            var resultValue = new MetaRequestResult<TResult>
            {
                data = default,
                success = false,
                error = string.Empty,
            };

            if (RemoteMetaService == null)
                return resultValue;
            
            var result = await RemoteMetaService.ExecuteAsync(contract);
            
            resultValue.data = result.model as TResult;
            resultValue.success = result.success;
            resultValue.error = result.error;
            
            return resultValue;
        }
        
    }
    
    [Serializable]
    public struct MetaRequestResult<TResult>
    {
        public TResult data;
        public bool success;
        public string error;
    }
}