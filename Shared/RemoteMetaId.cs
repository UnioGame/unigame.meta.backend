namespace Game.Modules.ModelMapping
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UniGame.Core.Runtime;
    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
#if UNITY_EDITOR
    using UnityEditor;
    using UniModules.Editor;
#endif
    
    [Serializable]
#if ODIN_INSPECTOR
    [ValueDropdown("@Game.Modules.ModelMapping.RemoteMetaId.GetBackendTypes()", 
        IsUniqueList = true, 
        DropdownTitle = "RemoteMetaId")]
#endif
    public partial struct RemoteMetaId
    {
        [SerializeField]
        public int value;

        #region static editor data

        private static ContractsConfigurationAsset _dataAsset;

#if UNITY_EDITOR

        [InitializeOnLoadMethod]
        private static void ResetRemoteMetaId() => _dataAsset = null;
        
#endif
        
        public static IEnumerable<ValueDropdownItem<RemoteMetaId>> GetBackendTypes()
        {
#if UNITY_EDITOR
            _dataAsset ??= AssetEditorTools.GetAsset<ContractsConfigurationAsset>();
            
            var items = _dataAsset;
            if (items == null)
            {
                yield return new ValueDropdownItem<RemoteMetaId>()
                {
                    Text = "EMPTY",
                    Value = (RemoteMetaId)0,
                };
                yield break;
            }

            foreach (var remoteItem in items.configuration.remoteMetaData)
            {
                yield return new ValueDropdownItem<RemoteMetaId>()
                {
                    Text = remoteItem.method,
                    Value = (RemoteMetaId)remoteItem.id,
                };
            }
#endif
            yield break;
        }

        public static string GetBackendTypeName(RemoteMetaId slotId)
        {
#if UNITY_EDITOR
            var types = GetBackendTypes();
            var filteredTypes = types
                .FirstOrDefault(x => x.Value == slotId);
            var slotName = filteredTypes.Text;
            return string.IsNullOrEmpty(slotName) ? string.Empty : slotName;
#endif
            return string.Empty;
        }
        
        #endregion

        public static implicit operator int(RemoteMetaId v) =>  v.value;

        public static explicit operator RemoteMetaId(int v) => new RemoteMetaId { value = v };

        public override string ToString() => value.ToString();

        public override int GetHashCode() => value;

        public RemoteMetaId FromInt(int data)
        {
            value = data;
            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj is RemoteMetaId mask)
                return mask.value == value;
            return false;
        }
    }
}