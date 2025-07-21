namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UniGame.Core.Runtime;
    using UniGame.Core.Runtime.SerializableType;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
    using UnityEditor;
    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [CreateAssetMenu(menuName = "UniGame/MetaBackend/Js Meta Provider", fileName = "Js Meta Provider")]
    public class JsMetaProviderAsset : BackendMetaServiceAsset
    {
#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        public JsMetaContractConfig config;

#if ODIN_INSPECTOR
        [InlineProperty]
        [HideLabel]
#endif
        public JsBridgeAgentBase bridgePrefab;
        
        public override async UniTask<IRemoteMetaProvider> CreateAsync(IContext context)
        {
            var jsMetaService = new JsMetaProvider(config, bridgePrefab);
            context.Publish<IRemoteMetaJsProvider>(jsMetaService);
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

#if ODIN_INSPECTOR
        [LabelText(text:"@Name")]
        [ValueDropdown(nameof(GetContracts))]
#endif
        public SType contract = new();
        
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