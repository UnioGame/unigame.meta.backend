namespace Game.Modules.Meta.Runtime
{
    using System;
    using System.Linq;
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
            var contractType = contract.GetType().FullName;
            var inputType = contract.InputType?.FullName ?? "null";
            var outputType = contract.OutputType?.FullName ?? "null";

            var key = $"{contractType}|{inputType}|{outputType}";
            return GetStableHash(key);
        }

        private static int GetStableHash(string str)
        {
            unchecked
            {
                return str.Aggregate((int)2166136261, (current, t) => (current ^ t) * 16777619);
            }
        }
        
    }
}