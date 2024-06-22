namespace MetaService.Shared.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sirenix.OdinInspector;
    using UniModules.Editor;
    using UnityEngine;

    [Serializable]
    public struct BackendType
    {
        public string Name;
        public int Id;

        [SerializeReference]
        public BackendMetaServiceAsset Provider;
    }

    [Serializable]
    [ValueDropdown("@MetaService.Shared.Data.BackendTypeId.GetBackendTypes()", IsUniqueList = true, DropdownTitle = "BackendType")]
    public partial struct BackendTypeId
    {
        [SerializeField]
        public int value;

        #region static editor data

        private static BackendTypeDataAsset _dataAsset;

        public static IEnumerable<ValueDropdownItem<BackendTypeId>> GetBackendTypes()
        {
#if UNITY_EDITOR
            _dataAsset ??= AssetEditorTools.GetAsset<BackendTypeDataAsset>();
            var types = _dataAsset;
            if (types == null)
            {
                yield return new ValueDropdownItem<BackendTypeId>()
                {
                    Text = "EMPTY",
                    Value = (BackendTypeId)0,
                };
                yield break;
            }

            foreach (var type in types.Types)
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

        public override bool Equals(object obj)
        {
            if (obj is BackendTypeId mask)
                return mask.value == value;

            return false;
        }
    }
}