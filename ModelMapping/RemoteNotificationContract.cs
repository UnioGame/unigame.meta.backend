namespace ModelMapping
{
    using System;
    using MetaService.Shared;

    public abstract class RemoteNotificationContract<TDto> : IRemoteNotificationContract
    {
        public virtual MetaNotification NotificationId => default;
        public virtual Type DtoType => typeof(TDto);
    }

    public interface IRemoteNotificationContract
    {
        public MetaNotification NotificationId { get; }
        public Type DtoType { get; }
    }
}