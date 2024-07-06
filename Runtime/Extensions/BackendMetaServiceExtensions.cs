namespace Extensions
{
    using Cysharp.Threading.Tasks;
    using DefaultNamespace;
    using MetaService.Shared;

    public static class BackendMetaServiceExtensions
    {
        
        public static async UniTask<MetaRequestResult<TModel>> 
            InvokeContractAsync<TModel>(this IBackendMetaService backendMetaService, IRemoteMetaCall contract) 
            where TModel : class
        {
            var result = await backendMetaService.InvokeAsync(contract);
            return new MetaRequestResult<TModel>
            {
                Model = result.Model as TModel,
                Success = result.Success,
                Error = result.Error
            };
        }
        
        public static async UniTask<MetaRequestResult<TModel>> 
            InvokeContractAsync<TModel>(this IBackendMetaService backendMetaService, int id, object payload) 
            where TModel : class
        {
            var result = await backendMetaService.InvokeAsync(id,payload);
            return new MetaRequestResult<TModel>
            {
                Model = result.Model as TModel,
                Success = result.Success,
                Error = result.Error
            };
        }
    }
    
    public struct MetaRequestResult<TModel>
    {
        public TModel Model;
        public bool Success;
        public string Error;
    }
}