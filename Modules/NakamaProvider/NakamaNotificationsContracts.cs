namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Nakama;
    using Newtonsoft.Json;

    [Serializable]
    public class NakamaNotificationsListContract : NakamaContract<NakamaNotificationsListContract, IApiNotificationList>
    {
        public int limit = 100;
        public string cursor = string.Empty;

        [JsonIgnore]
        public override string Path => "notifications_list";

        [JsonIgnore]
        public override object Payload => this;
    }

    [Serializable]
    public class NakamaNotificationsDeleteContract : NakamaContract<NakamaNotificationsDeleteContract, string>
    {
        public string[] ids = Array.Empty<string>();

        [JsonIgnore]
        public override string Path => "notifications_delete";

        [JsonIgnore]
        public override object Payload => this;
    }
}
