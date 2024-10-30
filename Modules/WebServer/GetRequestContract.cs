namespace Modules.WebServer
{
    using System;

    [Serializable]
    public class GetRequestContract<TInput,TOutput> : IGetRequestContract
    {
        public TInput payload;
        public string token = String.Empty;
        public string url;

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
    public class GetRequestContract : IGetRequestContract
    {
        public object payload;
        public string token = String.Empty;
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