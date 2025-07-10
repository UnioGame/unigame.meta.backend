namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFlow.Runtime;
    using MetaService.Runtime;
    using Nakama;
    using R3;
    using Shared;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.Rx;
    using UniModules.Runtime.Network;
    using UnityEngine;

    [Serializable]
    public class NakamaService : GameService, INakamaService
    {
        private NakamaSettings _nakamaSettings;
        private NakamaConnection _connection;
        private INakamaAuthenticate _authenticate;
        private List<string> _healthCheckUrls;
        private List<NakamaServerData> _nakamaServers;
        private RetryConfiguration _retryConfiguration;
        private LifeTime _sessionLifeTime;

        private ReactiveValue<ConnectionState>
            _state = new(ConnectionState.Disconnected);

        public NakamaService(NakamaSettings nakamaSettings,
            NakamaConnection connection, 
            INakamaAuthenticate authenticate)
        {
            _sessionLifeTime = new();
            _healthCheckUrls = new();
            _nakamaServers = new ();
            _nakamaSettings = nakamaSettings;
            _connection = connection;
            _authenticate = authenticate;

            //_sessionLifeTime.AddCleanUpAction(() => _sessionLifeTime.Terminate());
            
            var servers = _nakamaSettings.servers;
            
            foreach (var serverEndpoint in servers)
            {
                var uriBuilder = UriBuilderTool.CreateUriBuilder(serverEndpoint.host, 
                    serverEndpoint.port ,serverEndpoint.scheme);
                
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

        public async UniTask<MetaConnectionResult> ConnectAsync()
        {
            //await when connected to nakama server
            return new MetaConnectionResult()
            {
                Success = false,
            };
        }

        public async UniTask DisconnectAsync()
        {
            //signout
            return;
        }

        public bool IsContractSupported(IRemoteMetaContract command)
        {
            return false;
        }

        public async UniTask<RemoteMetaResult> ExecuteAsync(MetaContractData contractData)
        {
            try
            {
                //await client.AuthenticateDeviceAsync("<DeviceId>");
            }
            catch (Nakama.ApiResponseException ex)
            {
                GameLog.LogFormat("Error authenticating device: {0}:{1}", ex.StatusCode, ex.Message);
            }

            return new RemoteMetaResult()
            {
                success = false,
                data = null,
                error = string.Empty,
                id = string.Empty,
            };
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
        
        public async UniTask<NakamaConnectionResult> SignInAsync(INakamaAuthenticateData authenticateData)
        {
            var stateValue = _state.Value;
            if (stateValue == ConnectionState.Connected ||
                stateValue == ConnectionState.Connecting)
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
            
            var sessionResult = await _authenticate.AuthenticateAsync(client, authenticateData);
            var session = sessionResult.session;
            var connected = await ConnectAsync(socket, session);
            
            _connection.session.Value = session;
            _connection.token.Value = session.AuthToken;

            await GetUserProfileAsync();


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