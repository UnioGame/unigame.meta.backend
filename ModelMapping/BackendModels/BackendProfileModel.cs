namespace ModelMapping.BackendModels
{
    using System;
    using System.Collections.Generic;

    public class BackendProfileModel
    {
        public string Id { get; set; }
        public string Nickname { get; set; }
        public string AvatarUrl { get; set; }
        public uint Silver { get; set; }
        public Dictionary<string, object> Artefacts { get; set; }
        public Dictionary<string, object> UnlockedHeroes { get; set; }
        public ushort Glc { get; set; }
        public byte Level { get; set; }
        public ushort Energy { get; set; }
        public byte Rank { get; set; }
        public List<string> Achievements { get; set; }
        public DateTime SubscribeUntil { get; set; }
        public List<object> BlockchainHistory { get; set; }
    }
}