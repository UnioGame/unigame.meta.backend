namespace Game.Modules.ModelMapping
{
    using System;
    using Meta.Runtime;
    using UniGame.MetaBackend.Shared;

    using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public class RemoteMetaDataConfig : IRemoteMetaDataConfiguration
    {
        [SerializeReference]
        public IRemoteDataConverter defaultConverter = new JsonRemoteDataConverter();

        public int contractHistorySize = 100;
        
#if ODIN_INSPECTOR
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        [ListDrawerSettings(ListElementLabelName = "method")]
#endif
        public RemoteMetaData[] remoteMetaData = Array.Empty<RemoteMetaData>();

        public IRemoteDataConverter Converter => defaultConverter;
        public RemoteMetaData[] RemoteMetaData => remoteMetaData;
        
        public int HistorySize => contractHistorySize;
        
        
        public string GetContractName(IRemoteMetaContract contract)
        {
            return GetRemoteMethodName(contract);
        }
        
        public string GetRemoteMethodName(IRemoteMetaContract contract)
        {
            return BackendMetaTools.GetContractName(contract);
        }

        public bool IsEmptyType(Type type)
        {
            var voidType = typeof(VoidRemoteData);
            var result = type == null || type == voidType|| type == typeof(string);
            return result;
        }
        
        public int CalculateMetaId(IRemoteMetaContract contract)
        {
            return BackendMetaTools.CalculateMetaId(contract);
        }
    }
}