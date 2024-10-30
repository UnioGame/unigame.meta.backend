namespace Modules.WebServer
{
    using System;

    [Serializable]
    public class PostRequestContract<TInput,TOutput> : IPostRequestContract
    {
        public TInput payload;
        public string token = string.Empty;
        public string url = string.Empty;

        public string Url
        {
            get => url; 
            set => url = value;
        }

        public string Token
        {
            get => token; 
            set => token = value;    
        }
        
        public object Payload => payload;
        public Type Output => typeof(TOutput);
        public Type Input => typeof(TInput);
    }
    
    [Serializable]
    public class PostRequestContract : IPostRequestContract
    {
        public object payload;
        public string token = string.Empty;
        public string url;
        public Type output;
        public Type input;
        
        public string Url
        {
            get => url; 
            set => url = value;
        }

        public string Token
        {
            get => token; 
            set => token = value;    
        }
        
        public object Payload => payload;
        public Type Output => output;
        public Type Input => input;
    }
}