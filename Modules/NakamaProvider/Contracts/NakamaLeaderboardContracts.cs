namespace UniGame.MetaBackend.Runtime.Contracts
{
    using System;
    using System.Collections.Generic;
    using Nakama;

    [Serializable]
    public class NakamaLeaderboardGetRecordsContract  : NakamaContract<string,string>
    {
        public string leaderboardId;
        public IEnumerable<string> ownerIds = null;
        public long? expiry = null;
        public int limit = 20;
        public string cursor = null;
        
        public override string Path =>  nameof(NakamaLeaderboardGetRecordsContract);
    }
    
    [Serializable]
    public class NakamaLeaderboardGetRecordsAroundContract  : NakamaContract<string,IApiLeaderboardRecordList>
    {
        public string leaderboardId;
        public string ownerId;
        public long? expiry = null;
        public int limit = 1;
        public string cursor = null;
        
        public override string Path =>  nameof(NakamaLeaderboardGetRecordsAroundContract);
    }
    
    [Serializable]
    public class NakamaLeaderboardWriteRecordContract  : NakamaContract<string,IApiLeaderboardRecord>
    {
        public string leaderboardId;
        public long score;
        public long subscore = 0L;
        public string metadata = null;
        public ApiOperator apiOperator = ApiOperator.NO_OVERRIDE;
        public override string Path =>  nameof(NakamaLeaderboardWriteRecordContract);
    }
}