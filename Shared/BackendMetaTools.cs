namespace Game.Modules.Meta.Runtime
{
    using System;
    using UniGame.MetaBackend.Shared;

    public static class BackendMetaTools
    {
        public const string ContractKey = "Contract";
        
        public static string GetContractName(IRemoteMetaContract contract)
        {
            
            if(contract == null) return string.Empty;
            if(!string.IsNullOrEmpty(contract.MethodName))
                return contract.MethodName;
            
            var typeName = contract.GetType().Name;
            if (typeName.EndsWith(ContractKey, StringComparison.OrdinalIgnoreCase) && 
                typeName.Length > ContractKey.Length)
            {
                return typeName.Substring(0,typeName.Length - ContractKey.Length);
            }

            return typeName;
        }
        
        public static int CalculateMetaId(IRemoteMetaContract contract)
        {
            var contractType = contract.GetType().Name;
            var inputType = contract.InputType?.GetHashCode() ?? 0;
            var outputType = contract.OutputType?.GetHashCode() ?? 0;
            var id = HashCode.Combine(contractType, inputType, outputType);
            return id;
        }
        
    }
}