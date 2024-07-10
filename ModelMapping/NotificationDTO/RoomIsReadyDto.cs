namespace ModelMapping.NotificationDTO
{
    using System;
    using Newtonsoft.Json;

    [Serializable]
    public class RoomIsReadyDto
    {
        [JsonProperty("room_id")]
        public string roomId;
    }
}