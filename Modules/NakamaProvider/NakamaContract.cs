namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Shared;

    [Serializable]
    public abstract class NakamaContract<TInput,TOutput> : RemoteMetaContract<TInput,TOutput>, 
        INakamaContract
    {

    }
}