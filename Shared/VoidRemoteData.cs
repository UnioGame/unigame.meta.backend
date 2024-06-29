namespace MetaService.Shared
{
    using System;

    [Serializable]
    public class VoidRemoteData
    {
        public static readonly VoidRemoteData Empty = new VoidRemoteData();
    }
}