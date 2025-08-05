namespace UniGame.MetaBackend.Runtime
{
    using System;

    [Serializable]
    public struct NakamaContractResult
    {
        public bool success;
        public string error;
        public Object data;
    }
}