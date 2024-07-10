namespace MetaService.Shared.Data
{
    using Nakama;

    public sealed class MetaNotificationResult
    {
        public MetaNotification Code { get; set; }
        public string Content { get; set; }
        public string CreateTime { get; set; }
        public string Id { get; set; }
        public bool Persistent { get; set; }
        public string SenderId { get; set; }
        public string Subject { get; set; }
        
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