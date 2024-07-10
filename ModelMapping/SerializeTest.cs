namespace ModelMapping
{
    using System;
    using BackendModels;
    using Newtonsoft.Json;
    using NotificationDTO;
    using Sirenix.OdinInspector;
    using UnityEngine;

    public class SerializeTest : MonoBehaviour
    {
        [Button("Log")]
        public void Log()
        {
            var backendProfileModel = new BackendProfileModel
            {
                Id = "test_id",
                Nickname = "tres_poli",
                AvatarUrl = String.Empty,
                Silver = 10000,
                Glc = 1000,
                Level = 255,
                Energy = 10000,
                Rank = 255,
                SubscribeUntil = DateTime.MaxValue
            };

            var sessionTokenDto = new SessionTokenDto
            {
                sessionToken = "MBjQoiq7MY3h1lsphDPYpJe3vA4O1lEOwV8IeehbZWstW0Py8uKXHB6bEwGSOKjR"
            };

            var serverInfoDto = new ServerInfoDto
            {
                ip = "127.0.0.1",
                port = 7777
            };

            var roomIsReadyDto = new RoomIsReadyDto
            {
                roomId = "test_room_id"
            };

            Debug.Log(JsonConvert.SerializeObject(backendProfileModel));
            Debug.Log(JsonConvert.SerializeObject(sessionTokenDto));
            Debug.Log(JsonConvert.SerializeObject(serverInfoDto, Formatting.Indented));
            Debug.Log(JsonConvert.SerializeObject(roomIsReadyDto));
        }
    }
}