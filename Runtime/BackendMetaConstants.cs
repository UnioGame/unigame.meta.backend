namespace MetaService.Runtime
{
    using UniGame.MetaBackend.Shared.Data;

    public class BackendMetaConstants
    {
        public static readonly MetaDataResult UnsupportedContract = new()
        {
            hash = -1,
            resultType = typeof(string),
            error = "Unsupported contract for Provider",
        };
    }
}