namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFlow.Runtime;
    using MetaService.Runtime;
    using Nakama;
    using Newtonsoft.Json;
    using R3;
    using Shared;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.DateTime;
    using UniGame.Runtime.Rx;
    using UniModules.Runtime.Network;
    using UnityEngine;

    [Serializable]
    public class NakamaService : GameService, INakamaService,INakamaAuthenticate
    {
        public static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
        };
        
        private NakamaSettings _nakamaSettings;
        private NakamaConnection _connection;
        private List<string> _healthCheckUrls;
        private List<NakamaServerData> _nakamaServers;
        private RetryConfiguration _retryConfiguration;
        private LifeTime _sessionLifeTime;

        private ReactiveValue<ConnectionState> _state;
        
        
        public NakamaService(NakamaSettings nakamaSettings, NakamaConnection connection)
        {
            _sessionLifeTime = new();
            _healthCheckUrls = new();
            _nakamaServers = new();
            _nakamaSettings = nakamaSettings;
            _connection = connection;
            _state = connection.state;
            
            var servers = _nakamaSettings.servers;

            foreach (var serverEndpoint in servers)
            {
                var uriBuilder = UriBuilderTool.CreateUriBuilder(serverEndpoint.host,
                    serverEndpoint.port, serverEndpoint.scheme);

                var nakamaServerRecord = new NakamaServerData();

                nakamaServerRecord.endpoint = serverEndpoint;
                nakamaServerRecord.url = uriBuilder.ToString();
                uriBuilder.Path = _nakamaSettings.healthCheckPath;
                nakamaServerRecord.healthCheckUrl = uriBuilder.ToString();

                _nakamaServers.Add(nakamaServerRecord);
                _healthCheckUrls.Add(nakamaServerRecord.healthCheckUrl);
            }

            _retryConfiguration = new RetryConfiguration(
                _nakamaSettings.retryDelayMs,
                _nakamaSettings.maxRetries);
        }

        public ReadOnlyReactiveProperty<ConnectionState> State => _state;
        
        public bool IsConnected => _state.Value == ConnectionState.Connected &&
                                   _connection.socket.Value is { IsConnected: true };
        
        public async UniTask<MetaConnectionResult> ConnectAsync()
        {
            if (_state.Value != ConnectionState.Connected)
            {
                return new MetaConnectionResult()
                {
                    Success = false,
                    Error = "Not connected to Nakama server",
                    State = _state.Value,
                };
            }

            return new MetaConnectionResult()
            {
                Error = string.Empty,
                State = _state.Value,
                Success = true,
            };
        }

        public async UniTask DisconnectAsync()
        {
            //signout
            await SignOutAsync();
        }

        public bool IsContractSupported(IRemoteMetaContract command)
        {
            return command is INakamaContract;
        }

        public async UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData contractData)
        {
            var result = new RemoteMetaResult()
            {
                success = false,
                data = null,
                error = string.Empty,
                id = contractData.contractName,
            };
            
            try
            {
                var state = _connection.state.Value;
                if (state != ConnectionState.Connected)
                {
                    var message = $"Cannot execute contract {contractData.contractName} {contractData.contract.GetType().Name} in state {state}";
                    result.error = message;
                }
                var session = _connection.session.Value;
                var client = _connection.client.Value;
                
                var contract = contractData.contract;
                var rpcName = contract.Path;
                var payloadObject = contract.Payload;
                var targetType = contract.OutputType;
                
                var payloadValue = string.Empty;
                if (payloadObject != null && payloadObject is not string)
                    payloadValue = JsonConvert.SerializeObject(payloadObject,JsonSettings);    
                
                var rpcResult = await client
                    .RpcAsync(session, rpcName, payloadValue);

                result.data = targetType == typeof(string) 
                    ? rpcResult.Payload 
                    : JsonConvert.DeserializeObject(rpcResult.Payload, targetType, JsonSettings);
                
                result.data ??= string.Empty;
                result.success = true;
            }
            catch (ApiResponseException ex)
            {
                var message = $"Error authenticating device: {ex.StatusCode}:{ex.Message}";
                GameLog.LogError(message);
                result.error = message;
            }

            return result;
        }

        public bool TryDequeue(out RemoteMetaResult result)
        {
            result = new RemoteMetaResult()
            {
                data = null,
                error = string.Empty,
                id = string.Empty,
                success = false
            };
            return false;
        }

        /// <summary>
        /// sign out from Namaka server and clear all authentication data
        /// </summary>
        public async UniTask<bool> SignOutAsync()
        {
            var isConnected = _state.Value != ConnectionState.Connected;
            
            //clear cached authentication data
            PlayerPrefs.DeleteKey(NakamaConstants.NakamaAuthenticateKey);

            if (!isConnected) return false;
            
            var session = _connection.session.Value;
            var socket = _connection.socket.Value;
            
            if (socket is { IsConnected: true })
            {
                await socket.CloseAsync();
            }

            _connection.Reset();
            
            return true;
        }

        /// <summary>
        /// sign to Namaka server with provided authentication data for start executing contracts
        /// </summary>
        public async UniTask<NakamaConnectionResult> SignInAsync(INakamaAuthenticateData authenticateData)
        {
            var stateValue = _state.Value;
            if (stateValue is ConnectionState.Connected or ConnectionState.Connecting)
            {
                return new NakamaConnectionResult()
                {
                    success = false,
                    error = "Already connected to Nakama server.",
                };
            }

            _state.Value = ConnectionState.Connecting;
            
            var connectionResult = await ConnectToServerAsync(authenticateData);
            
            _state.Value = connectionResult.success 
                ? ConnectionState.Connected 
                : ConnectionState.Disconnected;

            return connectionResult;
        }

        
        /// <summary>
        ///  Authenticate user by custom data. Try to convert authdata into supported type.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="authenticateData"></param>
        /// <returns>NakamaSessionResult</returns>
        public async UniTask<NakamaSessionResult> AuthenticateAsync(IClient client,
            INakamaAuthenticateData authenticateData)
        {
            var restoreResult = await RestoreSessionAsync(authenticateData.AuthTypeName, client);
            if(restoreResult.success) return restoreResult;
			
            ISession nakamaSession = null;
	        
            if (authenticateData is NakamaIdAuthenticateData idData)
            {
                // get a new refresh token
                nakamaSession = await client.AuthenticateDeviceAsync(idData.clientId,
                    idData.userName,
                    idData.create,idData.vars,
                    idData.retryConfiguration);
            }

            if (nakamaSession == null)
            {
                return new NakamaSessionResult()
                {
                    session = null,
                    error = $"Cannot authenticate user by {authenticateData.AuthTypeName}",
                    success = false
                };
            }

            var nakamaSessionData = new NakamaSessionData()
            {
                authToken = nakamaSession.AuthToken,
                authType = authenticateData.AuthTypeName,
                refreshToken = nakamaSession.RefreshToken,
                timestamp = DateTime.Now.ToUnixTimestamp()
            };

            var prefsValue = JsonConvert.SerializeObject(nakamaSessionData);
	        
            PlayerPrefs.SetString(NakamaConstants.NakamaAuthenticateKey,prefsValue);
	        
            var result = new NakamaSessionResult()
            {
                session = nakamaSession,
                error = string.Empty,
                success = true,
            };

            return result;
        }
        
        public async UniTask<IApiAccount> GetUserProfileAsync()
        {
            try
            {
                var client = _connection.client.Value;
                var session = _connection.session.Value;
                var account = await client.GetAccountAsync(session);
                
                _connection.account.Value = account;
                _connection.userId.Value = account.User.Id;
                
                return account;
            }
            catch (ApiResponseException e)
            {
                Debug.LogError("Error getting user account: " + e.Message);
                return null;
            }
        }

        private async UniTask<NakamaConnectionResult> ConnectToServerAsync(INakamaAuthenticateData authenticateData)
        {
            var connectionInitResult = await InitializeConnectionAsync();
            if (connectionInitResult.success == false)
                return connectionInitResult;
            
            var client = _connection.client.Value;
            var socket = _connection.socket.Value;
            
            var sessionResult = await AuthenticateAsync(client, authenticateData);
            var session = sessionResult.session;
            var connected = await ConnectAsync(socket, session);
            
            _connection.session.Value = session;
            _connection.token.Value = session.AuthToken;
            _connection.account.Value = await GetUserProfileAsync();
            
            return new NakamaConnectionResult()
            {
                success = connected,
                error = string.Empty,
            };
        }

        private async UniTask<NakamaConnectionResult> InitializeConnectionAsync()
        {
            var hostSettings = await SelectServerAsync();
            if (hostSettings == null)
            {
                return new NakamaConnectionResult()
                {
                    success = false,
                    error = "No available Nakama server found. Try again later.",
                };
            }
            
            var endpoint = hostSettings.endpoint;
            
            var client = new Client(endpoint.scheme, endpoint.host, 
                endpoint.port, endpoint.serverKey, UnityWebRequestAdapter.Instance);
            
            client.Timeout = _nakamaSettings.timeoutSec;
            client.GlobalRetryConfiguration = _retryConfiguration;
    
            var useMainThread = _nakamaSettings.useSocketMainThread;
#if UNITY_WEBGL
            useMainThread = true; // WebGL does not support multithreading for sockets
#endif
            
            var socket = client.NewSocket(useMainThread: useMainThread);
            
            var disposable = Observable
                .FromEvent(x => socket.Closed += ReconnectNakamaSocket, x => socket.Closed -= ReconnectNakamaSocket)
                .Subscribe();

            _sessionLifeTime.AddCleanUpAction(() =>
            {
                disposable.Dispose();
                socket.Closed -= ReconnectNakamaSocket;
                socket.CloseAsync();
            });
            
            _connection.client.Value = client;
            _connection.socket.Value = socket;
            _connection.serverData.Value = hostSettings;

            return new NakamaConnectionResult()
            {
                success = true,
                error = string.Empty,
            };
        }
        
        private async UniTask<NakamaServerData> SelectServerAsync()
        {
            var bestServer = await UrlChecker.SelectFastestEndPoint(_healthCheckUrls);
            if (bestServer.success == false)
                return null;
            
            var hostSettings = _nakamaServers
                .Find(x => x.healthCheckUrl == bestServer.url);

            return hostSettings;
        }
        
        private void ReconnectNakamaSocket()
        {
            var socket = _connection.socket.Value;
            var session = _connection.session.Value;
            ConnectAsync(socket, session).Forget();
        }
        
        private async UniTask<bool> ConnectAsync(ISocket socket, ISession session)
        {
            if(_sessionLifeTime.IsTerminated) return false;
            
            try
            {
                if (!socket.IsConnected)
                {
                    await socket.ConnectAsync(session)
                        .AsUniTask();
                    await UniTask.SwitchToMainThread();
                }
            }
            catch (Exception e)
            {
                GameLog.LogError("Error connecting socket: " + e.Message);
                return false;
            }

            return socket.IsConnected;
        }
        
        
        
        private async UniTask<NakamaSessionResult> RestoreSessionAsync(string authType, IClient client)
        {
            var result = new NakamaSessionResult()
            {
                session = null,
                error = $"cannot restore session by {authType}",
                success = false,
            };
	        
            var authData = PlayerPrefs.GetString(NakamaConstants.NakamaAuthenticateKey, string.Empty);

            if (string.IsNullOrEmpty(authData)) return result;

            var restoreData = JsonConvert.DeserializeObject<NakamaSessionData>(authData);
            if (!restoreData.authType.Equals(authType))
                return result;
	        
            var session = await RestoreSessionAsync(restoreData.authToken, restoreData.refreshToken, client);
	        
            if (session == null) return result;
	        
            return new NakamaSessionResult()
            {
                session = session,
                error = string.Empty,
                success = true,
            };
        }

        private async UniTask<ISession> RestoreSessionAsync(string authToken,string refreshToken, IClient client)
        {
            var session = Session.Restore(authToken, refreshToken);
	        
            // Check whether a session is close to expiry.
            if (!session.HasExpired(DateTime.UtcNow.AddSeconds(_nakamaSettings.refreshTokenInterval))) 
                return session;
	        
            try
            {
                // get a new access token
                session = await client.SessionRefreshAsync(session,canceller:LifeTime.Token);
                return session;
            }
            catch (ApiResponseException ex)
            {
                GameLog.LogError($"Nakama Restore Session Status {ex.StatusCode} GRPC {ex.GrpcStatusCode} Message {ex.Message}");	   
            }

            return null;
        }

    }

    [Serializable]
    public class NakamaServerData
    {
        public NakamaEndpoint endpoint;
        public string url;
        public string healthCheckUrl;
    }

    [Serializable]
    public struct NakamaConnectionResult
    {
        public bool success;
        public string error;
    }
}