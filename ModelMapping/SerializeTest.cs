namespace ModelMapping
{
    using System;
    using BackendModels;
    using Sirenix.OdinInspector;
    using Unity.Plastic.Newtonsoft.Json;
    using UnityEngine;

    public class SerializeTest : MonoBehaviour
    {
        [Button("Log")]
        public void Log()
        {
            var obj = new BackendProfileModel
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

            Debug.Log(JsonConvert.SerializeObject(obj));
        }
    }
}