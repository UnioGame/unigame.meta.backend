namespace ModelMapping.NotificationDTO
{
    using System;
    using Newtonsoft.Json;

    [Serializable]
    public class SessionTokenDto
    {
        [JsonProperty("SessionToken")]
        public string sessionToken;
    }
}