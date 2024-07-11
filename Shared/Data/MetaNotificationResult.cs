namespace MetaService.Shared.Data
{
    using Nakama;

    public sealed class MetaNotificationResult
    {
        public MetaNotification Code;
        public string Content;
        public string CreateTime;
        public string Id;
        public bool Persistent;
        public string SenderId;
        public string Subject;
        
        public static MetaNotificationResult Map(IApiNotification notification)
        {
            return new MetaNotificationResult
            {
                Code = (MetaNotification)notification.Code,
                Content = notification.Content,
                Id = notification.Id,
                Persistent = notification.Persistent,
                Subject = notification.Subject,
                CreateTime = notification.CreateTime,
                SenderId = notification.SenderId
            };
        }
    }
}