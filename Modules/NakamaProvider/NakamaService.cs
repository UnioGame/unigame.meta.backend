namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Contracts;
    using Cysharp.Threading.Tasks;
    using GameFlow.Runtime;
    using MetaService.Runtime;
    using Nakama;
    using Newtonsoft.Json;
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
        private List<string> _healthCheckUrls;
        private List<NakamaServerData> _nakamaServers;
        private RetryConfiguration _retryConfiguration;
        private LifeTime _sessionLifeTime;

        private ReactiveValue<ConnectionState> _state = new();


        public NakamaService(NakamaSettings nakamaSettings, NakamaConnection connection)
        {
            _sessionLifeTime = new();
            _healthCheckUrls = new();
            _nakamaServers = new();
            _nakamaSettings = nakamaSettings;
            _connection = connection;
            _connection.RestoreSessionData();
            _state.Value = ConnectionState.Disconnected;
            
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
        
        public async UniTask<MetaConnectionResult> ConnectAsync()
        {
            return new MetaConnectionResult()
            {
                Error = string.Empty,
                State = ConnectionState.Connected,
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

        public async UniTask<ContractMetaResult> ExecuteAsync(
            MetaContractData contractData,
            CancellationToken cancellationToken = default)
        {
            var result = new ContractMetaResult()
            {
                success = false,
                data = null,
                error = string.Empty,
                id = contractData.contractName,
            };

            try
            {
                var contract = contractData.contract;
                var contractResult = await ExecuteContractAsync(_connection, contract, cancellationToken);

                result.data = contractResult.data;
                result.success = contractResult.success;
                result.error = contractResult.error;
            }
            catch (ApiResponseException ex)
            {
                var message = $"Error authenticating device: {ex.StatusCode}:{ex.Message}";
                GameLog.LogError(message);
                result.error = message;
            }

            return result;
        }

        public async UniTask<ContractMetaResult> ExecuteContractAsync(NakamaConnection connection,
            IRemoteMetaContract contract,
            CancellationToken cancellation = default)
        {
            var contractResult = new ContractMetaResult()
            {
                success = false,
                data = default,
                error = string.Empty,
            };

            try
            {
                return contract switch
                {
                    INakamaAuthContract authContract => (await AuthContractAsync(authContract, cancellation))
                        .ToContractResult(),
                    NakamaUsersContract usersContract => await LoadUsersAsync(usersContract, connection, cancellation),
                    NakamaAccountContract accountContract => await LoadAccountAsync(connection, cancellation),
                    NakamaLeaderboardGetRecordsContract getLeaderboardRecordsContract => await GetLeaderboardAsync(
                        connection, getLeaderboardRecordsContract, cancellation),
                    NakamaLeaderboardGetRecordsAroundContract leaderboardRecordsAround => await
                        GetLeaderboardAroundAsync(connection, leaderboardRecordsAround, cancellation),
                    NakamaLeaderboardWriteRecordContract writeLeaderboardRecordContract => await WriteLeaderboardAsync(
                        connection, writeLeaderboardRecordContract, cancellation),
                    NakamaTournamentsListContract tournamentsListContract => await GetTournamentsAsync(connection,
                        tournamentsListContract, cancellation),
                    NakamaJoinTournamentsContract joinTournament => await JoinTournamentAsync(connection, joinTournament,
                        cancellation),
                    NakamaTournamentRecordsContract tournamentRecords => await ListTournamentRecordsAsync(connection,
                        tournamentRecords, cancellation),
                    NakamaTournamentRecordsAroundContract tournamentAroundRecords => await
                        ListTournamentRecordsAroundAsync(connection, tournamentAroundRecords, cancellation),
                    NakamaTournamentWriteRecordContract writeTournamentRecord => await TournamentWriteAsync(connection,
                        writeTournamentRecord, cancellation),
                    _ => await ExecuteRpcContractAsync(connection, contract, cancellation)
                };
            }
            catch (Exception e)
            {
                GameLog.LogError(e);
                contractResult.error = e.Message;
                contractResult.success = false;
                contractResult.data = string.Empty;
            }

            return contractResult;
        }

        public async UniTask<ContractMetaResult> DeviceIdAuthAsync(
            NakamaDeviceIdAuthContract contract,
            CancellationToken cancellation = default)
        {
            var connectionResult = new NakamaServiceResult()
            {
                success = false,
                error = string.Empty,
            };
            
            try
            {
                var authData = contract.authData;
                var loginData = new NakamaDeviceIdAuthenticateData()
                {
                    clientId = authData.clientId,
                    create = authData.create,
                    retryConfiguration = authData.retryConfiguration ?? _retryConfiguration,
                    userName = authData.userName,
                    vars = authData.vars,
                };
                connectionResult = await AuthenticateAsync(loginData,cancellation:cancellation);
            }
            catch (ApiResponseException ex)
            {
                connectionResult.error = ex.Message;
                connectionResult.success = false;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = connectionResult,
                success = connectionResult.success,
                error = connectionResult.error,
            };
        }
        
        public async UniTask<ContractMetaResult> WriteLeaderboardAsync(
            NakamaConnection connection,
            NakamaLeaderboardWriteRecordContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;

            IApiLeaderboardRecord leaderboard = null;
            var error = string.Empty;

            try
            {
                leaderboard = await client
                    .WriteLeaderboardRecordAsync(session, contract.leaderboardId,
                        contract.score,
                        contract.subscore, contract.metadata,
                        contract.apiOperator,
                        _retryConfiguration,
                        cancellation);
            }
            catch (ApiResponseException ex)
            {
                leaderboard = null;
                error = ex.Message;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = leaderboard,
                success = leaderboard != null,
                error = error,
            };
        }

        public async UniTask<ContractMetaResult> GetTournamentsAsync(
            NakamaConnection connection,
            NakamaTournamentsListContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;

            IApiTournamentList leaderboard = null;
            var error = string.Empty;

            try
            {
                leaderboard = await client.ListTournamentsAsync(session,
                    contract.categoryStart,
                    contract.categoryEnd,
                    contract.startTime,
                    contract.endTime,
                    contract.limit,
                    contract.cursor,
                    _retryConfiguration,
                    cancellation);
            }
            catch (ApiResponseException ex)
            {
                leaderboard = null;
                error = ex.Message;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = leaderboard,
                success = leaderboard != null,
                error = error,
            };
        }

        public async UniTask<ContractMetaResult> JoinTournamentAsync(
            NakamaConnection connection,
            NakamaJoinTournamentsContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;

            var error = string.Empty;
            var result = (string)null;
            
            try
            {
                await client.JoinTournamentAsync(session,
                    contract.tournamentId,
                    _retryConfiguration,
                    cancellation);
                result = string.Empty;
            }
            catch (ApiResponseException ex)
            {
                error = ex.Message;
                result = null;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = result,
                success = result != null,
                error = error,
            };
        }
        
        
        public async UniTask<ContractMetaResult> ListTournamentRecordsAsync(
            NakamaConnection connection,
            NakamaTournamentRecordsContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;
            var error = string.Empty;

            IApiTournamentRecordList result = null;
            
            try
            {
                result = await client.ListTournamentRecordsAsync(session,
                    contract.tournamentId,
                    contract.ownerIds,
                    contract.expiry,
                    contract.limit,
                    contract.cursor,
                    _retryConfiguration,
                    cancellation);
            }
            catch (ApiResponseException ex)
            {
                error = ex.Message;
                result = null;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = result,
                success = result != null,
                error = error,
            };
        }
        
        public async UniTask<ContractMetaResult> ListTournamentRecordsAroundAsync(
            NakamaConnection connection,
            NakamaTournamentRecordsAroundContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;
            var error = string.Empty;

            IApiTournamentRecordList result = null;
            
            try
            {
                result = await client.ListTournamentRecordsAroundOwnerAsync(session,
                    contract.tournamentId,
                    contract.ownerId,
                    contract.expiry,
                    contract.limit,
                    contract.cursor,
                    _retryConfiguration,
                    cancellation);
            }
            catch (ApiResponseException ex)
            {
                error = ex.Message;
                result = null;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = result,
                success = result != null,
                error = error,
            };
        }
        
        public async UniTask<ContractMetaResult> TournamentWriteAsync(
            NakamaConnection connection,
            NakamaTournamentWriteRecordContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;
            var error = string.Empty;

            IApiLeaderboardRecord result = null;
            
            try
            {
                result = await client.WriteTournamentRecordAsync(session,
                    contract.tournamentId,
                    contract.score,
                    contract.subScore,
                    contract.metadata,
                    contract.apiOperator,
                    _retryConfiguration,
                    cancellation);
            }
            catch (ApiResponseException ex)
            {
                error = ex.Message;
                result = null;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = result,
                success = result != null,
                error = error,
            };
        }
        
        public async UniTask<ContractMetaResult> GetLeaderboardAsync(
            NakamaConnection connection,
            NakamaLeaderboardGetRecordsContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;

            IApiLeaderboardRecordList leaderboard = null;
            var error = string.Empty;

            try
            {
                leaderboard = await client
                    .ListLeaderboardRecordsAsync(session, contract.leaderboardId,
                        contract.ownerIds, contract.expiry,
                        contract.limit, contract.cursor,
                        _retryConfiguration,
                        cancellation);
            }
            catch (ApiResponseException ex)
            {
                leaderboard = null;
                error = ex.Message;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = leaderboard,
                success = leaderboard != null,
                error = error,
            };
        }

        public async UniTask<ContractMetaResult> GetLeaderboardAroundAsync(
            NakamaConnection connection,
            NakamaLeaderboardGetRecordsAroundContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;

            IApiLeaderboardRecordList leaderboard = null;
            var error = string.Empty;

            try
            {
                leaderboard = await client
                    .ListLeaderboardRecordsAroundOwnerAsync(session, contract.leaderboardId,
                        contract.ownerId, contract.expiry,
                        contract.limit, contract.cursor,
                        _retryConfiguration,
                        cancellation);
            }
            catch (ApiResponseException ex)
            {
                leaderboard = null;
                error = ex.Message;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = leaderboard,
                success = leaderboard != null,
                error = error,
            };
        }

        public async UniTask<ContractMetaResult> ExecuteRpcContractAsync(
            NakamaConnection connection,
            IRemoteMetaContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;
            var rpcName = contract.Path;
            var payloadObject = contract.Payload;
            var targetType = contract.OutputType;

            var payloadValue = string.Empty;
            if (payloadObject != null && payloadObject is not string)
                payloadValue = JsonConvert.SerializeObject(payloadObject, INakamaService.JsonSettings);

            var contractResult = new ContractMetaResult()
            {
                success = false,
                data = default,
                error = string.Empty,
            };

            var rpcResult = await client
                .RpcAsync(session, rpcName, payloadValue, _retryConfiguration, cancellation);

#if UNITY_EDITOR
            if (_nakamaSettings.enableLogging)
            {
                GameLog.Log($"[NakamaService] RPC '{rpcName}' executed. Payload: {payloadValue} Result: \n{rpcResult.Payload}",Color.aquamarine );
            }
#endif
            
            var resultObject = targetType == typeof(string)
                ? rpcResult.Payload
                : JsonConvert.DeserializeObject(rpcResult.Payload, targetType, INakamaService.JsonSettings);

            var success = resultObject != null;
            
            if (resultObject == null && contract is IFallbackContract fallbackContract)
            {
                resultObject = JsonConvert.DeserializeObject(rpcResult.Payload, fallbackContract.FallbackType, INakamaService.JsonSettings);
            }
            
            contractResult.success = success;
            contractResult.data = resultObject;
            contractResult.error = string.Empty;

            return contractResult;
        }

        private async UniTask<ContractMetaResult<NakamaAuthResult>> AuthContractAsync(
            INakamaAuthContract authContract, 
            CancellationToken cancellation = default)
        {
            var contractResult = new ContractMetaResult<NakamaAuthResult>()
            {
                success = false,
                data = default,
                error = string.Empty,
            };
            
            var authResult = await AuthenticateAsync(authContract.AuthData,cancellation);

            var session = _connection.session.Value;
            var created = session?.Created ?? false;

            var nakamaAuthResult = new NakamaAuthResult()
            {
                account = _connection.account.Value,
                created = created,
                error = authResult.error,
                success = authResult.success,
            };
            
            contractResult.success = authResult.success;
            contractResult.error = authResult.error;
            contractResult.data = nakamaAuthResult;
            return contractResult;
        }
               
        public async UniTask<NakamaServiceResult> AuthenticateAsync(INakamaAuthenticateData authenticateData, CancellationToken cancellation = default)
        {
            var serviceResult = await ConnectToServerAsync(cancellation);
            if (!serviceResult.success) return serviceResult;
            
            var client = _connection.client.Value;
            var socket = _connection.socket.Value;

            var sessionResult = await AuthenticateAsync(client,authenticateData,cancellation:cancellation);

            if (!sessionResult.success)
                return sessionResult;

            var session = _connection.session.Value;
            var connected = await ConnectAsync(socket, session);

            //if (connected) await GetUserProfileAsync();

            return new NakamaServiceResult()
            {
                success = connected,
                error = string.Empty,
            };
        }

        
        /// <summary>
        ///  Authenticate user by custom data. Try to convert authdata into supported type.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="authenticateData"></param>
        /// <param name="cancellation"></param>
        /// <returns>NakamaSessionResult</returns>
        private async UniTask<NakamaServiceResult> AuthenticateAsync(
            IClient client,
            INakamaAuthenticateData authenticateData,
            CancellationToken cancellation = default)
        {
            var session = _connection.session.CurrentValue;
            var restoreResult = session != null
                ? await RefreshSessionAsync(session, client)
                : await RefreshSessionAsync(client);

            session = _connection.session.Value;
            
            if (restoreResult.success && session.IsExpired == false)
            {
                _connection.UpdateSessionData(session);
                return restoreResult;
            }

            try
            {
                if (authenticateData is NakamaDeviceIdAuthenticateData idData)
                {
                    // get a new refresh token
                    session = await client.AuthenticateDeviceAsync(idData.clientId,
                        idData.userName,
                        idData.create, idData.vars,
                        idData.retryConfiguration,
                        canceller:cancellation);
                }
            }
            catch (ApiResponseException ex)
            {
                Debug.LogFormat("Error authenticating device: {0}:{1}", ex.StatusCode, ex.Message);
            }
            
            if (session == null)
            {
                return new NakamaServiceResult()
                {
                    error = $"Cannot authenticate user by {authenticateData.AuthTypeName}",
                    success = false
                };
            }
            
            _connection.authType.Value = authenticateData.AuthTypeName;
            _connection.UpdateSessionData(session);
            
            var result = new NakamaServiceResult()
            {
                error = string.Empty,
                success = true,
            };

            return result;
        }

        
        public async UniTask<ContractMetaResult> LoadUsersAsync(
            NakamaUsersContract usersContract,
            NakamaConnection connection,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;

            var contractResult = new ContractMetaResult()
            {
                success = false,
                data = default,
                error = string.Empty,
            };

            var account = await client
                .GetUsersAsync(session, usersContract.userIds, usersContract.userNames, usersContract.facebookIds,
                    _retryConfiguration, cancellation)
                .AsUniTask();

            contractResult.success = account != null;
            contractResult.data = account;
            contractResult.error = string.Empty;

            return contractResult;
        }

        public async UniTask<ContractMetaResult> LoadAccountAsync(
            NakamaConnection connection,
            CancellationToken cancellation = default)
        {
            var contractResult = new ContractMetaResult()
            {
                success = false,
                data = default,
                error = string.Empty,
            };

            var account = await GetUserProfileAsync();

            contractResult.success = account != null;
            contractResult.data = account;
            contractResult.error = string.Empty;
            
            return contractResult;
        }

        public bool TryDequeue(out ContractMetaResult result)
        {
            result = new ContractMetaResult()
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

            if (!isConnected) return false;

            var client = _connection.client.Value;
            var session = _connection.session.Value;
            var socket = _connection.socket.Value;
            
            if (client != null && session != null)
            {
                try
                {
                    client.SessionLogoutAsync(session)
                        .AsUniTask()
                        .Forget();
                }
                catch (Exception e)
                {
                    GameLog.LogException(e);
                }
            }
            
            try
            {
                if (socket is { IsConnected: true })
                    socket.CloseAsync().AsUniTask().Forget();
            }
            catch (Exception e)
            {
                GameLog.LogException(e);
            }
            
            _state.Value = ConnectionState.Disconnected;
            _connection.Reset();

            return true;
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
                _connection.userName.Value = account.User.Username;

                return account;
            }
            catch (ApiResponseException e)
            {
                Debug.LogError("Error getting user account: " + e.Message);
                return null;
            }
        }

        public async UniTask<NakamaServiceResult> ConnectToServerAsync(CancellationToken cancellation = default)
        {
            var client = _connection.client.Value;
            var socket = _connection.socket.Value;
            
            if (client != null && socket != null && (socket.IsConnected || socket.IsConnecting))
            {
                return new NakamaServiceResult()
                {
                    error = string.Empty,
                    success = true,
                };
            }
            
            _state.Value = ConnectionState.Connecting;
            
            var hostSettings = await SelectServerAsync(cancellation);
            if (hostSettings == null)
            {
                return new NakamaServiceResult()
                {
                    success = false,
                    error = "No available Nakama server found. Try again later.",
                };
            }

            var endpoint = hostSettings.endpoint;

            client = new Client(endpoint.scheme, endpoint.host,
                endpoint.port, endpoint.serverKey, UnityWebRequestAdapter.Instance,
                autoRefreshSession:_nakamaSettings.autoRefreshSession);

            client.Timeout = _nakamaSettings.timeoutSec;
            client.GlobalRetryConfiguration = _retryConfiguration;

            var useMainThread = _nakamaSettings.useSocketMainThread;
#if UNITY_WEBGL
            useMainThread = true; // WebGL does not support multithreading for sockets
#endif
            socket = client.NewSocket(useMainThread: useMainThread);

            var disposable = Observable
                .FromEvent(
                    x => socket.Closed += ReconnectNakamaSocket, 
                    x => socket.Closed -= ReconnectNakamaSocket)
                .Subscribe();

            _sessionLifeTime.AddCleanUpAction(() =>
            {
                disposable.Dispose();
                socket.Closed -= ReconnectNakamaSocket;
                socket.CloseAsync()
                    .AsUniTask()
                    .Forget();
            });

            _connection.client.Value = client;
            _connection.socket.Value = socket;
            _connection.serverData.Value = hostSettings;

            return new NakamaServiceResult()
            {
                success = true,
                error = string.Empty,
            };
        }

        private async UniTask<NakamaServerData> SelectServerAsync(CancellationToken cancellation = default)
        {
            var bestServer = await UrlChecker
                .SelectFastestEndPoint(_healthCheckUrls,cancellation:cancellation);
            
            if (bestServer.success == false)
                return null;

            var hostSettings = _nakamaServers
                .Find(x => x.healthCheckUrl == bestServer.url);

            return hostSettings;
        }

        private void ReconnectNakamaSocket(string reason)
        {
            var socket = _connection.socket.Value;
            var session = _connection.session.Value;
            ConnectAsync(socket, session).Forget();
        }

        private async UniTask<bool> ConnectAsync(ISocket socket, ISession session)
        {
            if (_sessionLifeTime.IsTerminated) return false;

            try
            {
                if (!socket.IsConnected)
                {
                    await socket.ConnectAsync(session).AsUniTask();
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


        private async UniTask<NakamaServiceResult> RefreshSessionAsync(ISession session, IClient client)
        {
            if (session == null)
            {
                return new NakamaServiceResult()
                {
                    error = "session is null",
                    success = false,
                };
            }
            
            try
            {
                // Check whether a session is close to expiry.
                if (session.IsExpired ||
                    session.HasExpired(DateTime.UtcNow.AddSeconds(_nakamaSettings.refreshTokenInterval)))
                {
                    // get a new access token
                    session = await client
                        .SessionRefreshAsync(session, canceller: LifeTime.Token)
                        .AsUniTask();
                }
                
                _connection.UpdateSessionData(session);
                
                return new NakamaServiceResult()
                {
                    error = string.Empty,
                    success = !session.IsExpired,
                };
            }
            catch (ApiResponseException ex)
            {
                GameLog.LogError($"Nakama Restore Session Status {ex.StatusCode} GRPC {ex.GrpcStatusCode} Message {ex.Message}");
                return new NakamaServiceResult()
                {
                    error = ex.Message,
                    success = false,
                };
            }
        }

        private async UniTask<NakamaServiceResult> RefreshSessionAsync(IClient client)
        {
            var sessionData = _connection.sessionData.Value;
            var refreshToken = sessionData.RefreshToken;
            var authToken = sessionData.AuthToken;
            var result = await RefreshSessionAsync(authToken, refreshToken, client);
            return result;
        }

        private async UniTask<NakamaServiceResult> RefreshSessionAsync(string authToken, string refreshToken, IClient client)
        {
            var session = Session.Restore(authToken, refreshToken);
            var result = await RefreshSessionAsync(session, client);
            return result;
        }
    }
}