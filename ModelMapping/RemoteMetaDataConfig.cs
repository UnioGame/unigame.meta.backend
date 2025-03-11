namespace Game.Modules.ModelMapping
{
    using System;
    using Meta.Runtime;
    using UniGame.MetaBackend.Shared;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [Serializable]
    public class RemoteMetaDataConfig : IRemoteMetaDataConfiguration
    {
        [SerializeReference]
        public IRemoteDataConverter defaultConverter = new JsonRemoteDataConverter();

        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        [ListDrawerSettings(ListElementLabelName = "method")]
        public RemoteMetaData[] remoteMetaData = Array.Empty<RemoteMetaData>();

        public IRemoteDataConverter Converter => defaultConverter;
        public RemoteMetaData[] RemoteMetaData => remoteMetaData;
        
        
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