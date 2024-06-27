namespace MetaService.Shared.Data
{
    using System;

    [Serializable]
    public struct RemoteMetaResult
    {
        public string Id;
        public string Data;
        public bool Success;
        public string Error;
    }
}