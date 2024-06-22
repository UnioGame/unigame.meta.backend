namespace MetaService.Shared
{
    using Cysharp.Threading.Tasks;
    using UniGame.Core.Runtime;

    public interface IBackendMetaSource
    {
        UniTask<IBackendMetaService> CreateAsync(IContext context);
    }
}