namespace Extensions
{
    using Cysharp.Threading.Tasks;
    using UniGame.MetaBackend.Shared;
    
#if UNITY_EDITOR
    using UnityEditor;
#endif
    
    public static class BackendMetaServiceExtensions
    {
        public static IBackendMetaService RemoteMetaService;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void Reset()
        {
            RemoteMetaService = null;
        }
#endif
        
        public static async UniTask<MetaRequestResult<TResult>> ExecuteAsync<TResult>(this IRemoteMetaContract contract) 
            where TResult : class
        {
            var resultValue = new MetaRequestResult<TResult>
            {
                Result = default,
                Success = false,
                Error = string.Empty,
            };

            if (RemoteMetaService == null)
                return resultValue;
            
            var result = await RemoteMetaService.ExecuteAsync(contract);
            
            resultValue.Result = result.model as TResult;
            resultValue.Success = result.success;
            resultValue.Error = result.error;
            
            return resultValue;
        }
    }
    
    public struct MetaRequestResult<TResult>
    {
        public TResult Result;
        public bool Success;
        public string Error;
    }
}