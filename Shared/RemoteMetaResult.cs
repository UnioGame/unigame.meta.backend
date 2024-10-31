namespace UniGame.MetaBackend.Shared.Data
{
    using System;
    using UnityEngine.Serialization;

    [Serializable]
    public struct RemoteMetaResult
    {
        public string Id;
        public object data;
        [FormerlySerializedAs("Success")]
        public bool success;
        [FormerlySerializedAs("Error")]
        public string error;
    }
    
    [Serializable]
    public struct RemoteMetaResult<TResult>
    {
        public string Id;
        public TResult Data;
        public bool Success;
        public string Error;
    }
}