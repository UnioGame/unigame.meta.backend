namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using System;

    [Serializable]
    public class NakamaSessionData
    {
        public string ConnectionId;
        public string AuthToken;
        public string RefreshToken;
        public string MatchmakerTicket;
        public long ExpireTime;
    }
}