namespace MetaService.Shared
{
    using System;
    using Newtonsoft.Json;
    using Sirenix.OdinInspector;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Core.Runtime.SerializableType;
    using UniModules.UniCore.Runtime.Utils;
    using UnityEngine;

    [Serializable]
    public class SimpleResultMetaCallContract<TOutput> : SimpleMetaCallContract<string, TOutput>
    {
    }
    
    [Serializable]
    public class SimpleMetaCallContract<TInput> : SimpleMetaCallContract<TInput, string>
    {
    }
    
    [Serializable]
    public class SimpleMetaCallContract<TInput, TOutput> : RemoteCallContract<TInput, TOutput>
    {
        public string method = string.Empty;

#if UNITY_EDITOR && ODIN_INSPECTOR
        [GUIColor(0f, 1f, 0f, 1f)]
        [InlineButton(nameof(PrintInputType), "Print json")]
#endif
        public SType input;
        
#if UNITY_EDITOR && ODIN_INSPECTOR
        [GUIColor(0f, 1f, 0f, 1f)]
        [InlineButton(nameof(PrintOutputType), "Print json")]
#endif
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
            output = typeof(TOutput);
        }
        
#if UNITY_EDITOR && ODIN_INSPECTOR
        private void PrintInputType()
        {
            var obj = InputType.CreateWithDefaultConstructor();
            GameLog.Log(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }

        private void PrintOutputType()
        {
            var obj = OutputType.CreateWithDefaultConstructor();
            GameLog.Log(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
#endif
    }
}