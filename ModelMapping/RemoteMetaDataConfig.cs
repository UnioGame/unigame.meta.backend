namespace Game.Modules.ModelMapping
{
    using System;
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
        public RemoteMetaData[] remoteMetaData = Array.Empty<RemoteMetaData>();

        public IRemoteDataConverter Converter => defaultConverter;
        public string GetMethodTemplate => getMethodTemplate;
        public string CommandMethodTemplate => postMethodTemplate;
        public RemoteMetaData[] RemoteMetaData => remoteMetaData;
    }
}