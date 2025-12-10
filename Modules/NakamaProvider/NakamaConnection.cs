namespace UniGame.MetaBackend.Runtime
{
    using System;
    using Nakama;
    using Newtonsoft.Json;
    using UniGame.Runtime.Rx;
    using UnityEngine;
    using ConnectionState = Shared.ConnectionState;

    [Serializable]
    public class NakamaConnection
    {
        /// <summary>
        /// Contains the user ID of the currently authenticated user.
        /// </summary>
        public ReactiveValue<string> userId = new();
        
        public ReactiveValue<string> userName = new();
        
        public ReactiveValue<string> authType = new();

        /// <summary>
        /// Contains the server data of the currently connected Nakama server.
        /// </summary>
        public ReactiveValue<NakamaServerData> serverData = new();

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
        public ReactiveValue<NakamaSessionData> sessionData = new();

        public void UpdateSessionData(ISession newSession)
        {
            var userIdValue = newSession?.UserId;
            var userNameValue = newSession?.Username;
            
            session.Value = newSession;
            userId.Value = userIdValue;
            userName.Value = userNameValue;

            var sessionValue = new NakamaSessionData()
            {
                RefreshToken = newSession?.RefreshToken,
                AuthToken = newSession?.AuthToken,
                UserId = userIdValue,
                Username = userNameValue,
                AuthType = authType.Value
            };

            sessionData.Value = sessionValue;
            
            PlayerPrefs.SetString(NakamaConstants.NakamaSessionDataKey,JsonConvert.SerializeObject(sessionValue));
        }

        public void RestoreSessionData()
        {
            if (!PlayerPrefs.HasKey(NakamaConstants.NakamaSessionDataKey))
                return;

            var sessionJson = PlayerPrefs.GetString(NakamaConstants.NakamaSessionDataKey);
            var sessionValue = JsonConvert.DeserializeObject<NakamaSessionData>(sessionJson);
            sessionData.Value = sessionValue;
            authType.Value = sessionValue.AuthType;
            userId.Value = sessionValue.UserId;
            userName.Value = sessionValue.Username;
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(NakamaConstants.NakamaSessionDataKey);
            
            session.Value = null;
            socket.Value = null;
            client.Value = null;
            account.Value = null;
            authType.Value = string.Empty;
            userId.Value = string.Empty;
            userName.Value = string.Empty;
            sessionData.Value = default;
            serverData.Value = null;
        }
    }
    
}