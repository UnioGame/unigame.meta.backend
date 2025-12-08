namespace UniGame.MetaBackend.Shared.Data
{
    using System;
    using UnityEngine.Serialization;

    [Serializable]
    public class MetaDataResult
    {
        public int id = -1;
        public int timestamp = 0;
        public int hash = 0;
        public Type resultType;
        public object payload = string.Empty;
        public object result = string.Empty;
        public object model = null;
        [FormerlySerializedAs("Success")]
        public bool success = false;
        [FormerlySerializedAs("Error")]
        public string error = string.Empty;
        public int statusCode = 200;
        
        public static readonly MetaDataResult Empty = new MetaDataResult()
        {
            hash = -1,
            resultType = typeof(string),
        };

    }
}