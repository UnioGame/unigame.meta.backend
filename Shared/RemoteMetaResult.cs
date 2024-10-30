namespace UniGame.MetaBackend.Shared.Data
{
    using System;

    [Serializable]
    public struct RemoteMetaResult
    {
        public string Id;
        public object Data;
        public bool Success;
        public string Error;
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