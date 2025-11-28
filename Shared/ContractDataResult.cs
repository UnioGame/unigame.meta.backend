namespace UniGame.MetaBackend.Runtime
{
    using System;

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
    }
}