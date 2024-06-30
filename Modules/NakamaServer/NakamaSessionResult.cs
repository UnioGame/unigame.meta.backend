namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using System;
    using global::Nakama;

    [Serializable]
    public struct NakamaSessionResult
    {
        public bool Success;
        public ISession Session;
        public string Error;
    }
}