namespace Game.Modules.ModelMapping
{
    using System;
    using global::ModelMapping;
    using MetaService.Shared;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [Serializable]
    public class RemoteMetaDataConfig : IRemoteMetaDataConfiguration
    {
        [SerializeReference]
        public IRemoteDataConverter defaultConverter = new JsonRemoteDataConverter();

        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        [ListDrawerSettings(ListElementLabelName = "method")]
        public RemoteMetaCallData[] remoteMetaData = Array.Empty<RemoteMetaCallData>();
        public RemoteMetaNotificationData[] remoteMetaNotificationData = Array.Empty<RemoteMetaNotificationData>();

        public IRemoteDataConverter Converter => defaultConverter;
        public RemoteMetaCallData[] RemoteMetaData => remoteMetaData;
        public RemoteMetaNotificationData[] RemoteMetaNotificationData => remoteMetaNotificationData;
        
        
        public string GetContractName(IRemoteCallContract contract)
        {
            return GetRemoteMethodName(contract);
        }
        
        public string GetRemoteMethodName(IRemoteCallContract contract)
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
        
        public int CalculateMetaId(IRemoteCallContract contract)
        {
            var contractType = contract.GetType().Name;
            var inputType = contract.InputType;
            var outputType = contract.OutputType;
            var id = HashCode.Combine(contractType, inputType.Name, outputType.Name);
            return id;
        }
        
    }
}