namespace MetaService.Shared.NotificationContracts
{
    using System;
    using ModelMapping;
    using UniGame.Core.Runtime.SerializableType;
    using UnityEditorInternal;

    [Serializable]
    public abstract class SimpleMetaNotificationContract<TDto> : RemoteNotificationContract<TDto>
    {
        public MetaNotification notificationId;
        public SType dtoType;

        public override MetaNotification NotificationId => notificationId;
        public override Type DtoType => dtoType;

        protected SimpleMetaNotificationContract(MetaNotification notificationId)
            : this()
        {
            this.notificationId = notificationId;
        }

        protected SimpleMetaNotificationContract()
        {
            dtoType = typeof(TDto);
        }
    }
}