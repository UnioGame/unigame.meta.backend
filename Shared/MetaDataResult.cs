namespace UniGame.MetaBackend.Shared.Data
{
    using System;

    [Serializable]
    public class MetaDataResult
    {
        public static readonly MetaDataResult Empty = new MetaDataResult()
        {
            hash = -1,
            resultType = typeof(string),
        };
        
        public int id = -1;
        public int timestamp = 0;
        public int hash = 0;
        public Type resultType;
        public object payload = string.Empty;
        public object result = string.Empty;
        public object model = null;
        public bool success = false;
        public string error = string.Empty;
    }
}