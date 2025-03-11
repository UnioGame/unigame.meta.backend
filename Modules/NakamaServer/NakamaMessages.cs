namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Shared.Data;

    public class NakamaMessages
    {
        public const string SockedConnectionError = "Socket connection error";
        public const string GetAccountError = "Get account error";
        public const string ConnectionInProgress = "Connection in progress";
        public const string NakamaServiceClosed = "Nakama service closed";
        public const string NakamaSessionError = "Nakama session error";
        public const string NotValidSessionState = "Nakama not valid session state";

        public static MetaConnectionResult NamakaSessionError = new()
        {
            Success = false,
            Error = NakamaSessionError,
            State = ConnectionState.Disconnected
        };
        
        public static MetaConnectionResult NamakaClosedResult = new()
        {
            Success = false,
            Error = NakamaServiceClosed,
            State = ConnectionState.Closed
        };
        
        public static MetaConnectionResult ConnectingResult = new()
        {
            Success = false,
            Error = ConnectionInProgress,
            State = ConnectionState.Connecting
        };
        
        public static MetaConnectionResult SocketErrorConnection = new()
        {
            Success = false,
            Error = SockedConnectionError,
            State = ConnectionState.Disconnected
        };
        
        public static MetaConnectionResult AccountErrorConnection = new()
        {
            Success = false,
            Error = GetAccountError,
            State = ConnectionState.Connected
        };

        public static MetaConnectionResult SuccessConnected = new()
        {
            Success = true,
            Error = string.Empty,
            State = ConnectionState.Connected
        };
    }
}