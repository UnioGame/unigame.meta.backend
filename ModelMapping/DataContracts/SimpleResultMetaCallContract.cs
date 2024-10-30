namespace UniGame.MetaBackend.Shared
{
    using System;
    using UniGame.Core.Runtime.SerializableType;

    [Serializable]
    public class SimpleResultMetaCallContract<TOutput> : SimpleMetaCallContract<string, TOutput>
    {
    }
    
    [Serializable]
    public class SimpleMetaCallContract<TInput> : SimpleMetaCallContract<TInput, string>
    {
    }
    
    [Serializable]
    public class SimpleMetaCallContract<TInput, TOuput> : RemoteCallContract<TInput, TOuput>
    {
        public string method = string.Empty;
        public SType input;
        public SType output;

        public override Type InputType => input;
        public override Type OutputType => output;
        public override string MethodName => method;

        public SimpleMetaCallContract(string method)
            : this()
        {
            this.method = method;
        }
        
        public SimpleMetaCallContract()
        {
            input = typeof(TInput);
            output = typeof(TOuput);
        }
    }
}