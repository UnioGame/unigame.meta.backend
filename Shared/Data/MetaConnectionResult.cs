namespace MetaService.Shared.Data
{
    using System;

    [Serializable]
    public struct MetaConnectionResult
    {
        public bool Success;
        public string Error;
        public ConnectionState State;

        public override string ToString()
        {
            return $"Success: {Success}, Error: {Error}, State: {State}";
        }
    }
}