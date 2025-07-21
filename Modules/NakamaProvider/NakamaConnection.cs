namespace UniGame.MetaBackend.Runtime
{
    using Nakama;
    using R3;
    using UniGame.Runtime.Rx;
    using ConnectionState = Shared.ConnectionState;

    public class NakamaConnection : INakamaConnection
    {
        /// <summary>
        /// Contains the user ID of the currently authenticated user.
        /// </summary>
        public ReactiveValue<string> userId = new();
        
        /// <summary>
        /// Contains the server data of the currently connected Nakama server.
        /// </summary>
        public ReactiveValue<NakamaServerData> serverData = new();
        
        /// <summary>
        /// Contains the current connection state of the client.
        /// </summary>
        public ReactiveValue<ConnectionState> state = new(ConnectionState.Disconnected);
        
        /// <summary>
        /// contains the last authentication data used to authenticate the user.
        /// </summary>
        public ReactiveValue<INakamaAuthenticateData> authenticateData = new();
        
        /// <summary>
        /// Used to establish connection between the client and the server.
        /// Contains a list of usefull methods required to communicate with Nakama server.
        /// Do not use this directly, use <see cref="Client"/> instead.
        /// </summary>
        public ReactiveValue<IClient> client = new();

        /// <summary>
        /// Used to communicate with Nakama server.
        ///
        /// For the user to send and receive messages from the server,
        /// <see cref="Session"/> must not be expired.
        ///
        /// Default expiration time is 60s, but for this demo we set it
        /// to 3 weeks (1 814 400 seconds).
        ///
        /// To initialize the session, call <see cref="AuthenticateDeviceIdAsync"/>
        /// or <see cref="AuthenticateFacebookAsync"/> methods.
        ///
        /// To reinitialize expired session, call <see cref="Reauthenticate"/> method.
        /// </summary>
        public ReactiveValue<ISession> session = new();

        /// <summary>
        /// Contains all the identifying data of a <see cref="Client"/>,
        /// like User Id, linked Device IDs, linked Facebook account, username, etc.
        /// </summary>
        public ReactiveValue<IApiAccount> account = new();

        /// <summary>
        /// Socket responsible for maintaining connection with Nakama
        /// server and exchanger realtime messages.
        ///
        /// Do not use this directly, use <see cref="Socket"/> instead.
        ///
        /// </summary>
        public ReactiveValue<ISocket> socket = new();
        
        /// <summary>
        /// Token used to authenticate the user.
        /// </summary>
        public ReactiveValue<string> token = new();

        public ReadOnlyReactiveProperty<IClient> Client => client;
        public ReadOnlyReactiveProperty<ISession> Session => session;
        public ReadOnlyReactiveProperty<ISocket> Socket => socket;
        
        public ReadOnlyReactiveProperty<IApiAccount> Account => account;
        
        public ReadOnlyReactiveProperty<string> Token => token;

        public ReadOnlyReactiveProperty<ConnectionState> State => state;
        
        public ReadOnlyReactiveProperty<INakamaAuthenticateData> AuthenticateData => authenticateData;

        public ReadOnlyReactiveProperty<NakamaServerData> ServerData => serverData;


        public void Reset()
        {
            session.Value = null;
            socket.Value = null;
            client.Value = null;
            account.Value = null;
            userId.Value = string.Empty;
            token.Value = string.Empty;
            authenticateData.Value = null;
            serverData.Value = null;
            state.Value = ConnectionState.Disconnected;
        }
    }
    
}