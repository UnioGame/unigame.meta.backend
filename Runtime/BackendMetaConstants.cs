namespace MetaService.Runtime
{
    using UniGame.MetaBackend.Runtime;

    public class BackendMetaConstants
    {
        public static readonly ContractDataResult UnsupportedContract = new()
        {
            hash = -1,
            resultType = typeof(string),
            error = "Unsupported contract for Provider",
        };
    }
}