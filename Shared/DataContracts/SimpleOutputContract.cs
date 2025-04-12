namespace UniGame.MetaBackend.Shared
{
    using System;
    using Newtonsoft.Json;
    using Sirenix.OdinInspector;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Core.Runtime.SerializableType;
    using UniModules.UniCore.Runtime.Utils;
    using UnityEngine;

    [Serializable]
    public class SimpleOutputContract<TOutput> : SimpleMetaContract<string, TOutput>
    {
    }
    
    [Serializable]
    public class SimpleInputContract<TInput> : SimpleMetaContract<TInput, string>
    {
    }
    
    [Serializable]
    public class SimpleMetaContract<TInput, TOutput> : RemoteMetaContract<TInput, TOutput>
    {
        [SerializeReference]
        public TInput inputData = default(TInput);
        
        public string path = string.Empty;

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
        public override string Path => path;
        public override object Payload => inputData;

        public SimpleMetaContract(string path)
            : this()
        {
            this.path = path;
        }
        
        public SimpleMetaContract()
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