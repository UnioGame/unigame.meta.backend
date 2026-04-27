namespace MetaService.Runtime
{
    using System;
    using Game.Modules.ModelMapping;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;

    public class BackendMetaConstants
    {
        public const int InitializationFailedStatusCode = -1001;
        public const int ProviderConnectionFailedStatusCode = -1002;
        public const int ProviderResolutionFailedStatusCode = -1003;
        public const int UnsupportedContractStatusCode = -1004;

        public const string InitializationFailedError = "BackendMetaService initialization failed";
        public const string ProviderConnectionFailedError = "Backend provider connection failed";
        public const string ProviderResolutionFailedError = "No provider supports the contract";
        public const string DefaultProviderMissingError = "No default backend provider was initialized";
        public const string NoProvidersAvailableError = "No backend providers were initialized";

        public static readonly ContractDataResult UnsupportedContract = new()
        {
            hash = -1,
            resultType = typeof(string),
            error = "Unsupported contract for Provider",
            statusCode = UnsupportedContractStatusCode,
        };

        public static ContractDataResult CreateFailureResult(
            IRemoteMetaContract contract,
            Game.Modules.ModelMapping.RemoteMetaData metaData,
            string contractName,
            string error,
            int statusCode,
            long timestamp)
        {
            var outputType = contract?.OutputType;
            outputType = outputType == null || outputType == typeof(VoidRemoteData)
                ? typeof(string)
                : outputType;

            return new ContractDataResult
            {
                contractId = contractName ?? string.Empty,
                metaId = metaData?.id ?? Game.Modules.ModelMapping.RemoteMetaData.Empty.id,
                payload = contract?.Payload ?? string.Empty,
                resultType = outputType,
                model = null,
                result = string.Empty,
                success = false,
                hash = -1,
                error = error ?? string.Empty,
                statusCode = statusCode,
                timestamp = timestamp,
            };
        }
    }
}