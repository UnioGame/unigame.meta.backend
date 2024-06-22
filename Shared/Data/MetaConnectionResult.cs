namespace MetaService.Shared.Data
{
    using System;

    [Serializable]
    public struct MetaConnectionResult
    {
        public bool Success;
        public string Error;
        public ConnectionState State;
    }
}