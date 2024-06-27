namespace MetaService.Shared.Data
{
    using System;

    [Serializable]
    public class MetaDataResult
    {
        public int Id = -1;
        public int Timestamp = 0;
        public int Hash = 0;
        public Type ResultType;
        public string Payload = string.Empty;
        public string Result = string.Empty;
        public object Model = null;
        public bool Success = false;
        public string Error = string.Empty;
        
        public static readonly MetaDataResult Empty = new MetaDataResult()
        {
            Hash = -1,
            ResultType = typeof(string),
        };
    }
}