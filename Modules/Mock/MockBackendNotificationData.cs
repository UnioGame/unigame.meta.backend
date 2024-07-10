namespace Game.Runtime.Services.Backend.Mock.Data
{
    using System;
    using MetaService.Shared;
    using Sirenix.OdinInspector;

    [Serializable]
    public class MockBackendNotificationData : ISearchFilterable
    {
        public MetaNotification NotificationId = default;
        public string Result = String.Empty;
        public bool Persistent;
        public string Subject;
        public string SenderId;
        
        public bool IsMatch(string searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return true;
            if(NotificationId.ToString().Contains(searchString,StringComparison.OrdinalIgnoreCase) ||
               Result.Contains(searchString,StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}