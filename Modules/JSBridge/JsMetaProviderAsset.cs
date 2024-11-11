namespace Game.Modules.unity.meta.backend.Modules.JSBridge
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
    using UniGame.Core.Runtime;
    using UniGame.Core.Runtime.SerializableType;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;
    using UnityEditor;
    using UnityEngine;

    [CreateAssetMenu(menuName = "UniGame/Services/MetaBackend/Js Meta Provider", fileName = "Js Meta Provider")]
    public class JsMetaProviderAsset : BackendMetaServiceAsset
    {
        [InlineProperty]
        [HideLabel]
        public JsMetaContractConfig config;
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var jsMetaService = new JsMetaProvider(config);
            return jsMetaService;
        }
    }

    [Serializable]
    public class JsMetaContractConfig
    {
        public List<JsMetaContractData> contracts = new();
    }

    [Serializable]
    public class JsMetaContractData
    {
        public int id;

        [ValueDropdown(nameof(GetContracts))]
        public SType contract;
        
        public static IEnumerable<ValueDropdownItem<SType>> GetContracts()
        {
#if UNITY_EDITOR
            var types = TypeCache.GetTypesDerivedFrom(typeof(IRemoteMetaContract));
            foreach (var type in types)
            {
                if(type.IsAbstract || type.IsInterface) continue;
                
                yield return new ValueDropdownItem<SType>()
                {
                    Text = type.Name,
                    Value = type,
                };
            }
#endif
            yield break;
        }
    }
}