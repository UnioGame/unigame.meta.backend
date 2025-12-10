namespace UniGame.MetaBackend.Runtime
{
    using System.Threading;
    using Contracts;
    using Cysharp.Threading.Tasks;
    using GameFlow.Runtime;
    using MetaService.Runtime;
    using Nakama;
    using Newtonsoft.Json;
    using R3;
    using Shared;

    public interface INakamaService : IRemoteMetaProvider, IGameService
    {
        static JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
        };

        ReadOnlyReactiveProperty<ConnectionState> State { get; }
        UniTask<MetaConnectionResult> ConnectAsync();
        UniTask DisconnectAsync();
        bool IsContractSupported(IRemoteMetaContract command);

        UniTask<ContractMetaResult> ExecuteAsync(
            MetaContractData contractData,
            CancellationToken cancellationToken = default);

        UniTask<NakamaContractResult> ExecuteContractAsync(NakamaConnection connection,
            IRemoteMetaContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> DeviceIdAuthAsync(
            NakamaDeviceIdAuthContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> WriteLeaderboardAsync(
            NakamaConnection connection,
            NakamaLeaderboardWriteRecordContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> GetTournamentsAsync(
            NakamaConnection connection,
            NakamaTournamentsListContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> JoinTournamentAsync(
            NakamaConnection connection,
            NakamaJoinTournamentsContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> ListTournamentRecordsAsync(
            NakamaConnection connection,
            NakamaTournamentRecordsContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> ListTournamentRecordsAroundAsync(
            NakamaConnection connection,
            NakamaTournamentRecordsAroundContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> TournamentWriteAsync(
            NakamaConnection connection,
            NakamaTournamentWriteRecordContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> GetLeaderboardAsync(
            NakamaConnection connection,
            NakamaLeaderboardGetRecordsContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> GetLeaderboardAroundAsync(
            NakamaConnection connection,
            NakamaLeaderboardGetRecordsAroundContract contract,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> ExecuteRpcContractAsync(
            NakamaConnection connection,
            IRemoteMetaContract contract,
            CancellationToken cancellation = default);
        
        UniTask<NakamaServiceResult> AuthenticateAsync(
            INakamaAuthenticateData authenticateData,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> LoadUsersAsync(
            NakamaUsersContract usersContract,
            NakamaConnection connection,
            CancellationToken cancellation = default);

        UniTask<NakamaContractResult> LoadAccountAsync(
            NakamaConnection connection,
            CancellationToken cancellation = default);

        bool TryDequeue(out ContractMetaResult result);

        /// <summary>
        /// sign out from Namaka server and clear all authentication data
        /// </summary>
        UniTask<bool> SignOutAsync();

        UniTask<IApiAccount> GetUserProfileAsync();
        UniTask<NakamaServiceResult> ConnectToServerAsync(CancellationToken cancellation = default);
    }
}