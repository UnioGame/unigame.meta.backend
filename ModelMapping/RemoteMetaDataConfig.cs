namespace Game.Modules.ModelMapping
{
    using System;
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
            if (string.IsNullOrEmpty(contract.MethodName) == false)
                return contract.MethodName;
            
            var contractType = contract.GetType();
            var contractName = contractType.Name;
            contractName = contractName.Replace(RemoteMetaConstants.ContractRemove, string.Empty);
            return contractName;
        }

        public bool IsEmptyType(Type type)
        {
            var voidType = typeof(VoidRemoteData);
            var result = type == null || type == voidType|| type == typeof(string);
            return result;
        }
        
        public int CalculateMetaId(IRemoteMetaContract contract)
        {
            var contractType = contract.GetType().Name;
            var inputType = contract.InputType?.GetHashCode() ?? 0;
            var outputType = contract.OutputType?.GetHashCode() ?? 0;
            var id = HashCode.Combine(contractType, inputType, outputType);
            return id;
        }
    }
}