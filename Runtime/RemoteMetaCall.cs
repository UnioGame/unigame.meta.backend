namespace DefaultNamespace
{
    using System;

    [Serializable]
    public struct RemoteMetaCall : IRemoteMetaCall
    {
        public object payload;
        public Type output;
        public Type input;
        
        public object Payload => payload;
        public Type Output => output;
        public Type Input => input;
    }
}