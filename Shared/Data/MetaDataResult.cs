namespace MetaService.Shared.Data
{
    using System;

    // [Serializable]
    // public class MetaDataResult<TModel>
    // {
    //     public int Id;
    //     public TModel Model;
    //     public string Data;
    //     public bool Success;
    //     public string Error;
    // }
    //
    [Serializable]
    public class MetaDataResult
    {
        public int Id;
        public int Hash;
        public string Data;
        public object Model;
        public bool Success;
        public string Error;
    }
}