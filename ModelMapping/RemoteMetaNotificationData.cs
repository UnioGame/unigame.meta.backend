namespace ModelMapping
{
    using System;
    using Game.Modules.ModelMapping;
    using MetaService.Shared.Data;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [Serializable]
    public class RemoteMetaNotificationData
    {
        public bool enabled = true;
        
        [ShowIf(nameof(enabled))]
        public BackendTypeId provider;
        
        [ShowIf(nameof(enabled))]
        [BoxGroup(nameof(contract))]
        [HideLabel]
        [InlineProperty]
        [SerializeReference]
        public IRemoteNotificationContract contract;
        
        [ShowIf(nameof(enabled))]
        [BoxGroup(nameof(converter))]
        public bool overriderDataConverter;
        
        [ShowIf(nameof(enabled))]
        [BoxGroup(nameof(converter))]
        [ShowIf(nameof(overriderDataConverter))]
        public IRemoteDataConverter converter;
    }
}