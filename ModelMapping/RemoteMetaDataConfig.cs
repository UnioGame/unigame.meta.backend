namespace Game.Modules.ModelMapping
{
    using System;
    using MetaService.Shared;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.Serialization;

    [Serializable]
    public class RemoteMetaDataConfig : IRemoteMetaDataConfiguration
    {
        [SerializeReference]
        public IRemoteDataConverter defaultConverter = new JsonRemoteDataConverter();
        
        [FormerlySerializedAs("remoteGetMethodTemplate")]
        public string getMethodTemplate = "Get{0}";
        [FormerlySerializedAs("remotePostMethodTemplate")]
        public string postMethodTemplate = "Post{0}";
        
        [Searchable(FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        [ListDrawerSettings(ListElementLabelName = "name")]
        public RemoteMetaCallData[] remoteMetaData = Array.Empty<RemoteMetaCallData>();

        public IRemoteDataConverter Converter => defaultConverter;
        public string GetMethodTemplate => getMethodTemplate;
        public string CommandMethodTemplate => postMethodTemplate;
        public RemoteMetaCallData[] RemoteMetaData => remoteMetaData;
        
        
        public string GetContractName(IRemoteCallContract contract)
        {
            var inputType = contract.InputType;
            var outputType = contract.OutputType;
            var contractName = $"out: {outputType.Name} arg: {inputType.Name}";
            return contractName;
        }
        
        public string GetRemoteMethodName(IRemoteCallContract contract)
        {
            if (string.IsNullOrEmpty(contract.MethodName) == false)
                return contract.MethodName;
            
            var outputType = contract.OutputType;
            var inputType = contract.InputType;
            var targetType = outputType;
            
            var isPostCommand = false;
            
            if (IsEmptyType(outputType) == false)
            {
                targetType = outputType;
                isPostCommand = false;
            }
            else if (IsEmptyType(inputType) == false)
            {
                targetType = inputType;
                isPostCommand = true;
            }
            else
            {
                targetType =  typeof(VoidRemoteData);
            }
            
            if(IsEmptyType(targetType))
                return RemoteMetaConstants.DefaultMethod;
            
            var typeName = targetType.Name;
            
            var methodTemplate = isPostCommand
                ? postMethodTemplate
                : getMethodTemplate;
                
            methodTemplate = string.IsNullOrEmpty(methodTemplate)
                ? RemoteMetaConstants.DefaultMethodTemplate
                : methodTemplate;
            
            return string.Format(methodTemplate, typeName);
        }

        public bool IsEmptyType(Type type)
        {
            var voidType = typeof(VoidRemoteData);
            var result = type == null || type == voidType|| type == typeof(string);
            return result;
        }
        
        public int CalculateMetaId(string contractName,IRemoteCallContract contract)
        {
            var inputType = contract.InputType;
            var outputType = contract.OutputType;
            var id = HashCode.Combine(contractName, inputType.Name, outputType.Name);
            return id;
        }
        
    }
}