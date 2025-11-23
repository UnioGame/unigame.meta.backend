namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Runtime;
    using Game.Modules.ModelMapping;

    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
#if UNITY_EDITOR
    using UniModules.Editor;
#endif
    
    [Serializable]
#if ODIN_INSPECTOR
    [ValueDropdown("@UniGame.MetaBackend.Runtime.BackendTypeId.GetBackendTypes()", IsUniqueList = true, DropdownTitle = "BackendType")]
#endif
       public struct BackendTypeId : IEquatable<int>
    {
        public static readonly BackendTypeId Empty = new() { value = nameof(Empty).GetHashCode() };
        
        [SerializeField]
        public int value;

        #region static editor data

        private static ContractsConfigurationAsset _dataAsset;

        public static IEnumerable<ValueDropdownItem<BackendTypeId>> GetBackendTypes()
        {
#if UNITY_EDITOR
            _dataAsset ??= AssetEditorTools.GetAsset<ContractsConfigurationAsset>();
            var types = _dataAsset.settings.backendTypes;
            
            if (types == null)
            {
                yield return new ValueDropdownItem<BackendTypeId>()
                {
                    Text = nameof(Empty),
                    Value = Empty,
                };
                yield break;
            }

            foreach (var type in types)
            {
                yield return new ValueDropdownItem<BackendTypeId>()
                {
                    Text = type.Name,
                    Value = (BackendTypeId)type.Id,
                };
            }
#endif
            yield break;
        }

        public static string GetBackendTypeName(BackendTypeId slotId)
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            _dataAsset = null;
        }

        #endregion

        public static implicit operator int(BackendTypeId v)
        {
            return v.value;
        }

        public static explicit operator BackendTypeId(int v)
        {
            return new BackendTypeId { value = v };
        }

        public override string ToString() => value.ToString();

        public override int GetHashCode() => value;

        public BackendTypeId FromInt(int data)
        {
            value = data;
            return this;
        }

        public bool Equals(int other)
        {
            return value == other;
        }

        public override bool Equals(object obj)
        {
            if (obj is BackendTypeId mask)
                return mask.value == value;

            return false;
        }
    }
}