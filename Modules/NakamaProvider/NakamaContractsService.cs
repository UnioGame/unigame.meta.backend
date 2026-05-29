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
    public class NakamaContractsService : GameService, INakamaService
    {
        private NakamaSettings _nakamaSettings;
        private NakamaConnection _connection;
        private List<string> _healthCheckUrls;
        private List<NakamaServerData> _nakamaServers;
        private RetryConfiguration _retryConfiguration;
        private LifeTime _sessionLifeTime;
        private SemaphoreSlim _recoverySemaphore = new(1, 1);

        private ReactiveValue<ConnectionState> _state = new();


        public NakamaContractsService(NakamaSettings nakamaSettings, NakamaConnection connection)
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
     
                uriBuilder.Path = serverEndpoint.healthCheckPath;
                uriBuilder.Port = serverEndpoint.healthCheckPort;
                nakamaServerRecord.healthCheckUrl =  uriBuilder.ToString();

                _nakamaServers.Add(nakamaServerRecord);
                _healthCheckUrls.Add(nakamaServerRecord.healthCheckUrl);
            }

            _retryConfiguration = new RetryConfiguration(
                _nakamaSettings.retryDelayMs,
                _nakamaSettings.maxRetries);
        }

        public ReadOnlyReactiveProperty<ConnectionState> State => _state;
        
        public bool IsAuthenticated => _connection.session.Value is { IsExpired: false };

        [Obsolete("ConnectAsync is deprecated for Nakama provider. Use ConnectToServerAsync and explicit authentication or session restore flow instead.")]
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
            if (IsRecoveryExcludedContract(contract))
                return await ExecuteContractCoreAsync(connection, contract, cancellation);

            return await ExecuteRecoverableContractAsync(connection, contract, cancellation);
        }

        private async UniTask<ContractMetaResult> ExecuteRecoverableContractAsync(
            NakamaConnection connection,
            IRemoteMetaContract contract,
            CancellationToken cancellation = default)
        {
            var readinessResult = await EnsureSessionAndSocketReadyAsync(cancellation);
            if (!readinessResult.success)
                return CreateFailureResult(contract, readinessResult.error, readinessResult.statusCode);

            var contractResult = await ExecuteContractCoreAsync(connection, contract, cancellation);
            if (!IsUnauthorized(contractResult))
                return contractResult;

            GameLog.LogError($"[NakamaService] Contract '{contract.Path}' returned 401. Running bounded session recovery.");

            var recoveryResult = await RecoverSessionAndSocketAsync(cancellation);
            if (!recoveryResult.success)
                return CreateFailureResult(contract, recoveryResult.error, recoveryResult.statusCode);

            var retryResult = await ExecuteContractCoreAsync(connection, contract, cancellation);
            if (IsUnauthorized(retryResult))
                GameLog.LogError($"[NakamaService] Retry after recovery still returned 401 for contract '{contract.Path}'.");

            return retryResult;
        }

        private async UniTask<ContractMetaResult> ExecuteContractCoreAsync(
            NakamaConnection connection,
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
                    INakamaAuthContract authContract => (await AuthContractAsync(authContract, cancellation)).ToContractResult(),
                    NakamaLogoutContract => (await SignOutContract()).ToContractResult(),
                    NakamaRestoreSessionContract restoreSessionContract => (await RestoreSessionAsync(cancellation)).ToContractResult(),
                    NakamaUsersContract usersContract => await LoadUsersAsync(usersContract, connection, cancellation),
                    NakamaAccountContract accountContract => await LoadAccountAsync(connection, cancellation),
                    NakamaUpdateAccountContract updateAccountContract => await UpdateAccountAsync(updateAccountContract.data,connection, cancellation),
                    NakamaLeaderboardGetRecordsContract getLeaderboardRecordsContract => await GetLeaderboardAsync(connection, getLeaderboardRecordsContract, cancellation),
                    NakamaLeaderboardGetRecordsAroundContract leaderboardRecordsAround => await GetLeaderboardAroundAsync(connection, leaderboardRecordsAround, cancellation),
                    NakamaLeaderboardWriteRecordContract writeLeaderboardRecordContract => await WriteLeaderboardAsync(connection, writeLeaderboardRecordContract, cancellation),
                    NakamaTournamentsListContract tournamentsListContract => await GetTournamentsAsync(connection, tournamentsListContract, cancellation),
                    NakamaJoinTournamentsContract joinTournament => await JoinTournamentAsync(connection, joinTournament, cancellation),
                    NakamaTournamentRecordsContract tournamentRecords => await ListTournamentRecordsAsync(connection, tournamentRecords, cancellation),
                    NakamaTournamentRecordsAroundContract tournamentAroundRecords => await ListTournamentRecordsAroundAsync(connection, tournamentAroundRecords, cancellation),
                    NakamaTournamentWriteRecordContract writeTournamentRecord => await TournamentWriteAsync(connection, writeTournamentRecord, cancellation),
                    NakamaDeleteAccountContract deleteAccount => await DeleteAccountAsync(connection, cancellation),
                    _ => await ExecuteRpcContractAsync(connection, contract, cancellation)
                };
            }
            catch (ApiResponseException ex)
            {
                GameLog.LogError($"[NakamaService] Contract '{contract?.Path}' failed with status {ex.StatusCode}: {ex.Message}");
                return CreateFailureResult(contract, ex.Message, (int)ex.StatusCode);
            }
            catch (Exception e)
            {
                GameLog.LogError(e);
                contractResult.error = e.Message;
                contractResult.success = false;
                contractResult.id = contract?.Path ?? string.Empty;
                contractResult.data = string.Empty;
            }

            return contractResult;
        }

        private bool IsRecoveryExcludedContract(IRemoteMetaContract contract)
        {
            return contract is INakamaAuthContract or NakamaRestoreSessionContract or NakamaLogoutContract;
        }

        private bool HasRestorableSession()
        {
            var session = _connection.session.Value;
            if (session != null)
                return true;

            var sessionData = _connection.sessionData.Value;
            return !string.IsNullOrEmpty(sessionData.AuthToken) &&
                   !string.IsNullOrEmpty(sessionData.RefreshToken);
        }

        private bool IsSessionValidForUse(ISession session)
        {
            if (session == null)
                return false;

            return !session.IsExpired &&
                   !session.HasExpired(DateTime.UtcNow.AddSeconds(_nakamaSettings.refreshTokenInterval));
        }

        private bool HasLiveSessionAndSocket()
        {
            var client = _connection.client.Value;
            var socket = _connection.socket.Value;
            var session = _connection.session.Value;

            return client != null &&
                   socket != null &&
                   IsSessionValidForUse(session) &&
                   (socket.IsConnected || socket.IsConnecting);
        }

        private async UniTask<NakamaServiceResult> EnsureSessionAndSocketReadyAsync(CancellationToken cancellation = default)
        {
            if (HasLiveSessionAndSocket())
                return CreateSuccessServiceResult();

            if (!HasRestorableSession())
            {
                return new NakamaServiceResult
                {
                    success = false,
                    error = "No active or restorable Nakama session found.",
                    statusCode = NakamaStatusCodes.InvalidSession,
                };
            }

            return await RecoverSessionAndSocketAsync(cancellation);
        }

        private async UniTask<NakamaServiceResult> RecoverSessionAndSocketAsync(CancellationToken cancellation = default)
        {
            await _recoverySemaphore.WaitAsync(cancellation);

            try
            {
                if (HasLiveSessionAndSocket())
                    return CreateSuccessServiceResult();

                if (!HasRestorableSession())
                {
                    return new NakamaServiceResult
                    {
                        success = false,
                        error = "No active or restorable Nakama session found.",
                        statusCode = NakamaStatusCodes.InvalidSession,
                    };
                }

                GameLog.Log("[NakamaService] Starting serialized session and socket recovery.");

                var restoreResult = await RestoreSessionAsync(cancellation);
                var restoreSuccess = restoreResult.success && restoreResult.data.success;

                if (!restoreSuccess)
                {
                    var restoreError = string.IsNullOrEmpty(restoreResult.error)
                        ? "Failed to restore Nakama session."
                        : restoreResult.error;

                    GameLog.LogError($"[NakamaService] Session recovery failed: {restoreError}");

                    return new NakamaServiceResult
                    {
                        success = false,
                        error = restoreError,
                        statusCode = restoreResult.statusCode,
                    };
                }

                GameLog.Log("[NakamaService] Session and socket recovery completed.", Color.green);
                return CreateSuccessServiceResult();
            }
            finally
            {
                _recoverySemaphore.Release();
            }
        }

        private static bool IsUnauthorized(ContractMetaResult result)
        {
            return !result.success && result.statusCode == 401;
        }

        private NakamaServiceResult CreateSuccessServiceResult()
        {
            return new NakamaServiceResult
            {
                success = true,
                error = string.Empty,
                statusCode = NakamaStatusCodes.Success,
            };
        }

        private ContractMetaResult CreateFailureResult(IRemoteMetaContract contract, string error, int statusCode = -1)
        {
            return new ContractMetaResult
            {
                id = contract?.Path ?? nameof(NakamaContractsService),
                data = null,
                success = false,
                error = error ?? string.Empty,
                statusCode = statusCode,
            };
        }

        private async UniTask<ContractMetaResult> DeleteAccountAsync(NakamaConnection connection, CancellationToken token)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;

            var result = new ContractMetaResult();

            try
            {
                await client.DeleteAccountAsync(session, canceller: token);
                PlayerPrefs.DeleteAll();
                _connection.Reset();
            }
            catch (ApiResponseException ex)
            {
                result.error = ex.Message;
                result.success = false;
                result.statusCode = (int)ex.StatusCode;
                result.data = false;
                return result;
            }
            catch (Exception e)
            {
                result.error = e.Message;
                result.success = false;
                result.data = false;
                return result;
            }
            
            result.success = true;
            result.data = true;
            return result;
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
            var statusCode = NakamaStatusCodes.Success;

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
                statusCode = (int)ex.StatusCode;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = leaderboard,
                success = leaderboard != null,
                error = error,
                statusCode = statusCode,
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
            var statusCode = NakamaStatusCodes.Success;

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
                statusCode = (int)ex.StatusCode;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = leaderboard,
                success = leaderboard != null,
                error = error,
                statusCode = statusCode,
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
            var statusCode = NakamaStatusCodes.Success;

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
                statusCode = (int)ex.StatusCode;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = result,
                success = result != null,
                error = error,
                statusCode = statusCode,
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
            var statusCode = NakamaStatusCodes.Success;

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
                statusCode = (int)ex.StatusCode;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = result,
                success = result != null,
                error = error,
                statusCode = statusCode,
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
            var statusCode = NakamaStatusCodes.Success;

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
                statusCode = (int)ex.StatusCode;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = result,
                success = result != null,
                error = error,
                statusCode = statusCode,
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
            var statusCode = NakamaStatusCodes.Success;

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
                statusCode = (int)ex.StatusCode;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = result,
                success = result != null,
                error = error,
                statusCode = statusCode,
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
            var statusCode = NakamaStatusCodes.Success;

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
                statusCode = (int)ex.StatusCode;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = leaderboard,
                success = leaderboard != null,
                error = error,
                statusCode = statusCode,
            };
        }

        public async UniTask<ContractMetaResult> GetLeaderboardAroundAsync(NakamaConnection connection,
            NakamaLeaderboardGetRecordsAroundContract contract,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;

            IApiLeaderboardRecordList leaderboard = null;
            var error = string.Empty;
            var statusCode = NakamaStatusCodes.Success;

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
                statusCode = (int)ex.StatusCode;
            }

            await UniTask.SwitchToMainThread();

            return new ContractMetaResult()
            {
                data = leaderboard,
                success = leaderboard != null,
                error = error,
                statusCode = statusCode,
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

            IApiRpc rpcResult;

            try
            {
                rpcResult = await client.RpcAsync(session, rpcName, payloadValue, _retryConfiguration, cancellation);
            }
            catch (ApiResponseException ex)
            {
                contractResult.error = ex.Message;
                contractResult.statusCode = (int)ex.StatusCode;
                return contractResult;
            }

#if UNITY_EDITOR
            if (_nakamaSettings.enableLogging)
            {
                GameLog.Log(
                    $"[NakamaService] RPC '{rpcName}' executed. Payload: {payloadValue} Result: \n{rpcResult.Payload}",
                    Color.aquamarine);
            }
#endif

            var resultObject = targetType == typeof(string)
                ? rpcResult.Payload
                : JsonConvert.DeserializeObject(rpcResult.Payload, targetType, INakamaService.JsonSettings);

            var success = resultObject != null;

            if (resultObject == null && contract is IFallbackContract fallbackContract)
            {
                resultObject = JsonConvert.DeserializeObject(rpcResult.Payload, fallbackContract.FallbackType,
                    INakamaService.JsonSettings);
            }

            contractResult.success = success;
            contractResult.data = resultObject;
            contractResult.error = string.Empty;
            contractResult.statusCode = NakamaStatusCodes.Success;

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

            var authResult = await AuthenticateAsync(authContract.AuthData, cancellation);

            var session = _connection.session.Value;
            var created = session?.Created ?? false;
            var success = authResult.success;
            var account = _connection.account.Value;

            if (success)
            {
                account = await GetUserProfileAsync();
                success = account != null;
            }

            var nakamaAuthResult = new NakamaAuthResult()
            {
                account = account,
                created = created,
                error = authResult.error,
                success = success,
            };

            contractResult.success = authResult.success;
            contractResult.error = authResult.error;
            contractResult.data = nakamaAuthResult;
            return contractResult;
        }

        public async UniTask<ContractMetaResult<NakamaAuthResult>> RestoreSessionAsync(
            CancellationToken cancellation = default)
        {
            var result = new ContractMetaResult<NakamaAuthResult>()
            {
                success = false,
                data = default,
                error = string.Empty,
                statusCode = -1,
                id = nameof(NakamaRestoreSessionContract),
            };

            var serviceResult = await ConnectToServerAsync(cancellation);
            if (!serviceResult.success) return result;

            var client = _connection.client.Value;
            var socket = _connection.socket.Value;

            var session = _connection.session.CurrentValue;
            var restoreResult = session != null
                ? await RefreshSessionAsync(session, client)
                : await RefreshSessionAsync(client);

            session = _connection.session.Value;

            result.success = restoreResult.success;
            result.error = restoreResult.error;
            result.statusCode = restoreResult.statusCode;

            if (restoreResult.success && !session.IsExpired)
            {
                _connection.UpdateSessionData(session);
            }
            else
            {
                return result;
            }

            var connected = await ConnectAsync(socket, session);

            result.success = connected;
            result.data = new NakamaAuthResult()
            {
                account = _connection.account.Value,
                created = false,
                error = string.Empty,
                success = connected,
            };

            return result;
        }

        public async UniTask<NakamaServiceResult> AuthenticateAsync(INakamaAuthenticateData authenticateData,
            CancellationToken cancellation = default)
        {
            var serviceResult = await ConnectToServerAsync(cancellation);
            if (!serviceResult.success) return serviceResult;

            var client = _connection.client.Value;
            var socket = _connection.socket.Value;

            var sessionResult = await AuthenticateAsync(client, authenticateData, cancellation: cancellation);

            if (!sessionResult.success)
                return sessionResult;

            var session = _connection.session.Value;
            var connected = await ConnectAsync(socket, session);

            return new NakamaServiceResult()
            {
                success = connected,
                error = string.Empty,
                statusCode = sessionResult.statusCode,
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
            var session = _connection.session.Value;
            var success = false;
#if GAME_DEBUG
            Debug.Log($"NAKAMA Auth Data: {authenticateData?.GetType().Name} {JsonConvert.SerializeObject(authenticateData)}");        
#endif
            
            try
            {
                switch (authenticateData)
                {
                    case NakamaDeviceIdAuthData idData:
                        // get a new refresh token
                        session = await client.AuthenticateDeviceAsync(idData.deviceId,
                            idData.userName,
                            idData.create, 
                            idData.vars,
                            idData.retryConfiguration,
                            canceller: cancellation);
                        break;
                    case NakamaIdAuthData idData:
                        // get a new refresh token
                        session = await client.AuthenticateCustomAsync(
                            idData.id,
                            idData.userName,
                            idData.create, idData.vars,
                            idData.retryConfiguration,
                            canceller: cancellation);
                        break;
                    case NakamaGoogleAuthenticateData googleData:
                        session = await PlayServicesAuthenticateAsync(googleData, cancellation);
                        break;
                    case NakamaFacebookAuthenticateData facebookData:
                        session = await FacebookAuthenticateAsync(facebookData, cancellation);
                        break;
                    case NakamaCustomAuthenticateData customData:
                        session = await CustomAuthenticateAsync(customData, cancellation);
                        break;
                }
                
                success = session != null;
            }
            catch (ApiResponseException ex)
            {
                success = false;
                GameLog.LogError($"Error authenticating device: {ex.StatusCode}:{ex.Message}");

                return new NakamaServiceResult()
                {
                    error = ex.Message,
                    success = false,
                    statusCode = (int)ex.StatusCode,
                };
            }

            if (success == false)
            {
                return new NakamaServiceResult()
                {
                    error = $"Cannot authenticate user by {authenticateData?.AuthTypeName}",
                    success = false,
                    statusCode = NakamaStatusCodes.InvalidSession,
                };
            }

            _connection.authType.Value = authenticateData.AuthTypeName;
            _connection.UpdateSessionData(session);

            var result = new NakamaServiceResult()
            {
                error = string.Empty,
                success = true,
                statusCode = 200,
            };

            return result;
        }
        
        public async UniTask<ISession> FacebookAuthenticateAsync(
            NakamaFacebookAuthenticateData data,
            CancellationToken cancellation = default)
        {
            var client = _connection.client.Value;
            var session = _connection.session.Value;
            
            //is we should link account, the session must be valid
            if (data.linkAccount && IsAuthenticated)
            {
                // get a new refresh token
                await client.LinkGoogleAsync(session, 
                    data.token,
                    data.retryConfiguration,
                    canceller: cancellation);
            }
            else
            {
                // get a new refresh token
                session = await client.AuthenticateFacebookAsync(data.token,
                    data.userName,
                    data.create,
                    data.import,
                    data.vars,
                    data.retryConfiguration,
                    canceller: cancellation);
            }

            return session;
        }
        
        public async UniTask<ISession> PlayServicesAuthenticateAsync(NakamaGoogleAuthenticateData data,
            CancellationToken cancellation = default)
        {
            var client = _connection.client.Value;
            var session = _connection.session.Value;
            
            //is we should link account, the session must be valid
            if (data.linkAccount && IsAuthenticated)
            {
                // get a new refresh token
                await client.LinkGoogleAsync(session, 
                    data.token,
                    data.retryConfiguration,
                    canceller: cancellation);
            }
            else
            {
                // get a new refresh token
                session = await client.AuthenticateGoogleAsync(data.token,
                    data.userName,
                    data.create,
                    data.vars,
                    data.retryConfiguration,
                    canceller: cancellation);
            }

            return session;
        }
        
        public async UniTask<ISession> CustomAuthenticateAsync(NakamaCustomAuthenticateData data,
            CancellationToken cancellation = default)
        {
            var client = _connection.client.Value;
            var session = _connection.session.Value;
            
            //is we should link account, the session must be valid
            if (data.linkAccount && IsAuthenticated)
            {
                // get a new refresh token
                await client.LinkCustomAsync(session, 
                    data.userId,
                    data.retryConfiguration,
                    canceller: cancellation);
            }
            else
            {
                // get a new refresh token
                session = await client.AuthenticateCustomAsync(
                    data.userId,
                    data.userName,
                    data.create,
                    data.vars,
                    data.retryConfiguration,
                    canceller: cancellation);
            }

            return session;
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
        
        public async UniTask<ContractMetaResult> UpdateAccountAsync(NakamaAccountData data,
            NakamaConnection connection,
            CancellationToken cancellation = default)
        {
            var client = connection.client.Value;
            var session = connection.session.Value;
            var account = connection.account.Value;
            var user = account?.User;
            
            var contractResult = new ContractMetaResult()
            {
                success = false,
                data = default,
                error = string.Empty,
            };

            if (account == null)
            {
                return contractResult;
            }
            
            var userId = session.UserId;
            var userName = session.Username;
            var displayName = string.IsNullOrEmpty(data.displayName) ? user.DisplayName : data.displayName;
            var avatarUrl = string.IsNullOrEmpty(data.avatarUrl) ? user.AvatarUrl : data.avatarUrl;

            try
            {
                await client.UpdateAccountAsync(session, userName, displayName, avatarUrl,canceller: cancellation)
                    .AsUniTask();
            }
            catch (Exception e)
            {
                GameLog.LogException(e);
                contractResult.error = e.Message;
                return contractResult;
            }
  
            contractResult.success = true;
            contractResult.data = account;

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

            var client = connection.client.Value;
            var session = connection.session.Value;

            if (client == null || session == null)
            {
                contractResult.error = "failed to load account | unauthorized";
                contractResult.statusCode = NakamaStatusCodes.InvalidSession;
                return contractResult;
            }

            IApiAccount account;

            try
            {
                account = await client.GetAccountAsync(session);
            }
            catch (ApiResponseException ex)
            {
                contractResult.error = ex.Message;
                contractResult.statusCode = (int)ex.StatusCode;
                return contractResult;
            }

            _connection.account.Value = account;
            _connection.userId.Value = account.User.Id;
            _connection.userName.Value = account.User.Username;
            
            contractResult.success = account != null;
            contractResult.data = account;
            contractResult.error = account == null ? "failed to load account | unauthorized" : string.Empty;
            contractResult.statusCode = account == null ? NakamaStatusCodes.InvalidSession : NakamaStatusCodes.Success;

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

        public async UniTask<ContractMetaResult<NakamaLogoutResult>> SignOutContract()
        {
            var signOutResult = await SignOutAsync();
            return new ContractMetaResult<NakamaLogoutResult>()
            {
                data = signOutResult,
                success = signOutResult.success,
                error = string.Empty,
            };
        }


        /// <summary>
        /// sign out from Namaka server and clear all authentication data
        /// </summary>
        public async UniTask<NakamaLogoutResult> SignOutAsync()
        {
            var isConnected = _state.Value != ConnectionState.Connected;

            if (!isConnected) return NakamaLogoutResult.Success;

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

            return NakamaLogoutResult.Success;
            ;
        }

        public async UniTask<IApiAccount> GetUserProfileAsync()
        {
            try
            {
                var client = _connection.client.Value;
                var session = _connection.session.Value;

                if (client == null || session == null)
                    return null;

                var account = await client.GetAccountAsync(session);

                _connection.account.Value = account;
                _connection.userId.Value = account.User.Id;
                _connection.userName.Value = account.User.Username;

                return account;
            }
            catch (ApiResponseException e)
            {
                GameLog.LogError("Error getting user account: " + e.Message);
                return null;
            }
        }

        public async UniTask<NakamaServiceResult> ConnectToServerAsync(CancellationToken cancellation = default)
        {
            var client = _connection.client.Value;
            var socket = _connection.socket.Value;

            if (client != null && socket != null && (socket.IsConnected || socket.IsConnecting))
            {
                _state.Value = ConnectionState.Connected;
                return new NakamaServiceResult()
                {
                    error = string.Empty,
                    success = true,
                    statusCode = NakamaStatusCodes.Success,
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
                autoRefreshSession: _nakamaSettings.autoRefreshSession);

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
                statusCode = NakamaStatusCodes.Success,
            };
        }

        private async UniTask<NakamaServerData> SelectServerAsync(CancellationToken cancellation = default)
        {
            var checkPointsCount = _healthCheckUrls.Count;
            if (checkPointsCount == 0) return null;

            var urlResult = new UrlResult();
            
            if (checkPointsCount == 1)
            {
                var url = _healthCheckUrls[0];
                urlResult.url = url;
                urlResult.success = true;
                urlResult.time = 0f;
            }
            else
            {
                urlResult = await UrlChecker.SelectFastestEndPoint(_healthCheckUrls, cancellation: cancellation);
            }
            

            if (!urlResult.success) return null;

            var hostSettings = _nakamaServers.Find(x => x.healthCheckUrl == urlResult.url);
            return hostSettings;
        }

        private void ReconnectNakamaSocket(string reason)
        {
            HandleSocketClosedAsync(reason).Forget();
        }

        private async UniTaskVoid HandleSocketClosedAsync(string reason)
        {
            if (_sessionLifeTime.IsTerminated)
                return;

            if (_state.Value == ConnectionState.Disconnected || !HasRestorableSession())
                return;

            GameLog.Log($"[NakamaService] Socket closed. Reason: {reason}");

            var recoveryResult = await RecoverSessionAndSocketAsync();
            if (!recoveryResult.success)
                GameLog.LogError($"[NakamaService] Closed-socket recovery failed: {recoveryResult.error}");
        }

        private async UniTask<bool> ConnectAsync(ISocket socket, ISession session)
        {
            if (_sessionLifeTime.IsTerminated) return false;

            if (socket == null || session == null)
            {
                _state.Value = ConnectionState.Disconnected;
                return false;
            }

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
                _state.Value = ConnectionState.Disconnected;
                GameLog.LogError("Error connecting socket: " + e.Message);
                return false;
            }

            _state.Value = socket.IsConnected ? ConnectionState.Connected : ConnectionState.Disconnected;
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
                    statusCode = NakamaStatusCodes.InvalidSession,
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
                    statusCode = NakamaStatusCodes.Success,
                };
            }
            catch (ApiResponseException ex)
            {
                GameLog.LogError(
                    $"Nakama Restore Session Status {ex.StatusCode} GRPC {ex.GrpcStatusCode} Message {ex.Message}");
                return new NakamaServiceResult()
                {
                    error = ex.Message,
                    success = false,
                    statusCode = (int)ex.StatusCode,
                };
            }
        }

        private async UniTask<NakamaServiceResult> RefreshSessionAsync(IClient client)
        {
            var sessionData = _connection.sessionData.Value;
            if (string.IsNullOrEmpty(sessionData.AuthToken) || string.IsNullOrEmpty(sessionData.RefreshToken))
            {
                return new NakamaServiceResult()
                {
                    success = false,
                    error = "session data is empty",
                    statusCode = NakamaStatusCodes.InvalidSession,
                };
            }

            var refreshToken = sessionData.RefreshToken;
            var authToken = sessionData.AuthToken;
            var result = await RefreshSessionAsync(authToken, refreshToken, client);
            return result;
        }

        private async UniTask<NakamaServiceResult> RefreshSessionAsync(string authToken, string refreshToken,
            IClient client)
        {
            var session = Session.Restore(authToken, refreshToken);
            var result = await RefreshSessionAsync(session, client);
            return result;
        }
    }
}