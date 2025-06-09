namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Core.Runtime;
    
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public class MockBackendData : ISearchFilterable
    {
        public string Method = String.Empty;
        public bool Success = false;
        public string Result = String.Empty;
        public string Error = string.Empty;
        
        public bool IsMatch(string searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return true;
            if(Method.Contains(searchString,StringComparison.OrdinalIgnoreCase) ||
               Error.Contains(searchString,StringComparison.OrdinalIgnoreCase) ||
               Result.Contains(searchString,StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}