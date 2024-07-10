namespace ModelMapping.NotificationDTO
{
    using System;
    using Newtonsoft.Json;

    [Serializable]
    public class ServerInfoDto
    {
        [JsonProperty("ip_address")]
        public string ip;
        [JsonProperty("port")]
        public int port;
    }
}