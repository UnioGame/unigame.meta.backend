namespace UniGame.MetaBackend.Runtime.Contracts
{
    using System;
    using System.Collections.Generic;
    using Nakama;

    // var categoryStart = 1;
    // var categoryEnd = 2;
    // var startTime = 1538147711;
    // var endTime = null; // all tournaments from the start time
    // var limit = 100; // number to list per page
    // var cursor = null;
    
    [Serializable]
    public class NakamaTournamentsListContract : NakamaContract<string, string>
    {
        public int categoryStart;
        public int categoryEnd;
        public int? startTime;
        public int? endTime;
        public int limit = 100;
        public string cursor = null;

        public override string Path => nameof(NakamaTournamentsListContract);
    }
    
    [Serializable]
    public class NakamaJoinTournamentsContract : NakamaContract<string, IApiTournamentList>
    {
        public string tournamentId;

        public override string Path => nameof(NakamaJoinTournamentsContract);
    }
    
    

    [Serializable]
    public class NakamaTournamentRecordsContract : NakamaContract<string, IApiTournamentRecordList>
    {
        public string tournamentId;
        public IEnumerable<string> ownerIds = null;
        public long? expiry = null;
        public int limit = 100;
        public string cursor = null;
        
        public override string Path => nameof(NakamaTournamentRecordsContract);
    }
    
    [Serializable]
    public class NakamaTournamentRecordsAroundContract : NakamaContract<string, IApiTournamentRecordList>
    {
        public string tournamentId;
        public string ownerId;
        public long? expiry = null;
        public int limit = 100;
        public string cursor = null;
        
        public override string Path => nameof(NakamaTournamentRecordsAroundContract);
    }
    
    [Serializable]
    public class NakamaTournamentWriteRecordContract : NakamaContract<string, IApiLeaderboardRecord>
    {
        public string tournamentId;
        public long score;
        public long subScore = 0;
        public string metadata = null;
        public ApiOperator apiOperator = ApiOperator.NO_OVERRIDE;
        
        public override string Path => nameof(NakamaTournamentWriteRecordContract);
    }
}