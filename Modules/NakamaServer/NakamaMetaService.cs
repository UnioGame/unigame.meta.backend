namespace Game.Runtime.Services.Backend.Nakama.Data
{
    using System;
    using Backend.Data;
    using Cysharp.Threading.Tasks;
    using global::Nakama;
    using MetaService.Shared;
    using MetaService.Shared.Data;
    using Newtonsoft.Json;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Core.Runtime;
    using UniModules.UniCore.Runtime.DataFlow;
    using UniRx;
    using UnityEngine;

    [Serializable]
    public class NakamaMetaService : IBackendMetaService
    {
        private NakamaConnectionData _connectionData;
        private IClient _client;
        private ISocket _socket;
        private ISession _session;
        private IApiAccount _account;

        private ConnectionState _connectionState;
        private LifeTimeDefinition _lifeTime;
        private RetryConfiguration _retryConfiguration;
        private IApiAccount _apiAccount;
        private IObservable<ISession> _sessionUpdated;
        private IObservable<Unit> _socketConnected;
        private IObservable<Unit> _socketClosed;
        private NakamaSessionData _nakamaSessionData;
        private bool _isReconnecting;

        public NakamaMetaService(NakamaConnectionData connectionData)
        {
            _connectionData = connectionData;
            _connectionState = ConnectionState.Disconnected;
            
            _retryConfiguration = new RetryConfiguration(
                _connectionData.retryDelayMs,
                _connectionData.retryCount,
                RetryLogging);

            _client = new Client(
                scheme:_connectionData.scheme, 
                host:_connectionData.host,
                port:_connectionData.port, 
                serverKey:_connectionData.serverKey,
                autoRefreshSession:_connectionData.autoRefreshSession,
                adapter:UnityWebRequestAdapter.Instance) 
            {
                Timeout = connectionData.requestTimeoutSec,
                GlobalRetryConfiguration = _retryConfiguration,
                Logger = new UnityLogger(),
            };
            
            _socket = _client.NewSocket(useMainThread: connectionData.useSocketMainThread);

            _sessionUpdated = Observable
                .FromEvent<ISession>(handler => _client.ReceivedSessionUpdated += handler,
                        handler => _client.ReceivedSessionUpdated -= handler);

            _socketConnected = Observable
                .FromEvent(handler => _socket.Connected += handler,
                    handler => _socket.Connected -= handler);
            
            _socketClosed = Observable.FromEvent(handler => _socket.Closed += handler,
                    handler => _socket.Closed -= handler);
                
            _socketClosed.Subscribe(OnSocketClosed).AddTo(_lifeTime);
            
            _socketConnected
                .Subscribe(OnSocketConnected)
                .AddTo(_lifeTime);
            
            _sessionUpdated
                .Subscribe(OnSessionUpdated)
                .AddTo(_lifeTime);
        }
        
        public ConnectionState State => _connectionState;
        
        public ILifeTime LifeTime => _lifeTime;
        
        public async UniTask<MetaConnectionResult> ConnectAsync(string deviceId)
        {
            if(_lifeTime.IsTerminated)
                return NakamaMessages.NamakaClosedResult;
            
            switch (_connectionState)
            {
                case ConnectionState.Connecting:
                    return NakamaMessages.ConnectingResult;
                case ConnectionState.Connected:
                    return NakamaMessages.SuccessConnected;
                case ConnectionState.Closed:
                    return NakamaMessages.NamakaClosedResult;
            }

            SetState(ConnectionState.Connecting);

            _nakamaSessionData = LoadLocalData();
            _nakamaSessionData.ConnectionId = deviceId;
            
            _session = await CreateSessionAsync(_nakamaSessionData);
            if (_session == null)
            {
                SetState(ConnectionState.Disconnected);
                return NakamaMessages.NamakaSessionError;
            }
            
            var connected = await SocketConnectAsync(_socket);
            if (!connected)
            {
                SetState(ConnectionState.Disconnected);
                return NakamaMessages.SocketErrorConnection;
            }
            
            SaveLocalData();
            SetState(ConnectionState.Connected);
            
            _apiAccount = await GetAccountAsync();

            if (_apiAccount == null)
            {
                return NakamaMessages.AccountErrorConnection;
            }
            
            return NakamaMessages.SuccessConnected;
        }

        public async UniTask DisconnectAsync()
        {
            await _socket.CloseAsync();
        }

        private async UniTask ReconnectAsync()
        {
            if(_isReconnecting) return;
            
            _isReconnecting = true;
            
            await UniTask.Yield();
            
            while (_lifeTime.IsTerminated == false && _connectionState == ConnectionState.Disconnected)
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(_connectionData.retryDelayMs));
                
                var result = await ConnectAsync(_nakamaSessionData.ConnectionId);
                if(result.Success) break;
            }
            
            _isReconnecting = false;
        }
        
        private async UniTask<ISession> CreateSessionAsync(NakamaSessionData sessionData)
        {
            var session = await RestoreSessionAsync(sessionData);
            if(session != null) return session;
            
            var newSession = await CreateSessionAsync(sessionData.ConnectionId);
            return newSession;
        }
        
        private void SaveLocalData()
        {
            _nakamaSessionData.AuthToken = _session.AuthToken;
            _nakamaSessionData.RefreshToken = _session.RefreshToken;
            _nakamaSessionData.ExpireTime = _session.ExpireTime;
            
            var stringData = JsonConvert.SerializeObject(_nakamaSessionData);
            PlayerPrefs.SetString(NakamaConstants.NakamaLocalKey, stringData);
        }
        
        private NakamaSessionData LoadLocalData()
        {
            if (PlayerPrefs.HasKey(NakamaConstants.NakamaLocalKey))
            {
                var stringData = PlayerPrefs.GetString(NakamaConstants.NakamaLocalKey);
                var localData = JsonConvert.DeserializeObject<NakamaSessionData>(stringData);
                return localData;
            }
            
            return new NakamaSessionData()
            {
                AuthToken = string.Empty,
                RefreshToken = string.Empty,
                ExpireTime = 0,
            };
        }
        
        private void OnSocketConnected()
        {
            SetState(ConnectionState.Connected);
        }
        
        private void OnSocketClosed()
        {
            SetState(ConnectionState.Disconnected);
            ReconnectAsync().Forget();
        }
        
        private void OnSessionUpdated(ISession session)
        {
            _session = session;
            _nakamaSessionData.AuthToken = session?.AuthToken;
            _nakamaSessionData.RefreshToken = session?.RefreshToken;
            _nakamaSessionData.ExpireTime = session?.ExpireTime ?? 0;
        }
        
        private void RetryLogging(int numRetry, Retry retry)
        {
            Debug.Log($"Nakama Service Retry: {numRetry} {retry}");
        }
        
        private async UniTask<ISession> CreateSessionAsync(string authId)
        {
            try
            {
                var authResult = await _client.AuthenticateCustomAsync(authId,canceller:_lifeTime.Token);
                return authResult;
            }
            catch (ApiResponseException ex)
            {
                SetState(ConnectionState.Disconnected);
                GameLog.LogError($"Error authenticating device:{ex.StatusCode} : {ex.Message}");
            }

            return null;
        }

        private async UniTask<bool> SocketConnectAsync(ISocket socket)
        {
            try
            {
                if (socket.IsConnected) return true;
                
                await socket
                    .ConnectAsync(
                        session:_session,
                        appearOnline: _connectionData.appearOnline,
                        connectTimeout: _connectionData.socketConnectTimeoutSec,
                        langTag: _connectionData.langTag)
                    .AsUniTask()
                    .AttachExternalCancellation(_lifeTime.Token);

                return socket.IsConnected;
            }
            catch (ApiResponseException ex)
            {
                GameLog.LogError($"Error connect to socket: {ex.StatusCode} : {ex.Message}");
            }

            return false;
        }

        private async UniTask<IApiAccount> GetAccountAsync()
        {
            try
            {
                var getAccountResult = await _client
                    .GetAccountAsync(_session,canceller: _lifeTime.Token);
                return getAccountResult;
            }
            catch (ApiResponseException ex)
            {
                GameLog.LogError($"Error get account: {ex.StatusCode} : {ex.Message}");
            }

            return null;
        }

        private async UniTask<ISession> RestoreSessionAsync(NakamaSessionData sessionData)
        {
            var isAuthToken = !string.IsNullOrEmpty(sessionData.AuthToken);
            if (!isAuthToken) return null;
            
            var session = Session.Restore(_nakamaSessionData.AuthToken, _nakamaSessionData.RefreshToken);
            if(session == null) return null;
            
            session = await RefreshSessionAsync(session);
            return session;
        }
        
        private async UniTask<ISession> RefreshSessionAsync(ISession currentSession)
        {
            var expiredTime = DateTime.UtcNow.AddSeconds(_connectionData.tokenExpireSec);
            var hasExpired = currentSession.HasExpired(expiredTime);
            if (!hasExpired) return null;

            try
            {
                var session = await _client.SessionRefreshAsync(_session, canceller: _lifeTime.Token);
                return session;
            }
            catch (ApiResponseException)
            {
                return null;
            }
        }

        private void SetState(ConnectionState newState)
        {
            if(_lifeTime.IsTerminated)
                _connectionState = ConnectionState.Closed;
            
            GameLog.Log("Backend state changed to: " + newState);

            switch (newState)
            {
                case ConnectionState.Disconnected:
                    break;
                case ConnectionState.Connecting:
                    break;
                case ConnectionState.Connected:
                    break;
                case ConnectionState.Closed:
                    break;
            }
            
            _connectionState = newState;
        }

        public void Dispose()
        {
            _lifeTime.Terminate();
            SetState(ConnectionState.Closed);
            DisconnectAsync().Forget();
        }
    }
}