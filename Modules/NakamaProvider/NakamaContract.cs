namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Shared;

    [Serializable]
    public class NakamaContract<TInput,TOutput> : RemoteMetaContract<TInput,TOutput>, 
        INakamaContract
    {
        
    }
}