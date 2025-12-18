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

        UniTask<ContractMetaResult> ExecuteContractAsync(NakamaConnection connection,
            IRemoteMetaContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> WriteLeaderboardAsync(
            NakamaConnection connection,
            NakamaLeaderboardWriteRecordContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> GetTournamentsAsync(
            NakamaConnection connection,
            NakamaTournamentsListContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> JoinTournamentAsync(
            NakamaConnection connection,
            NakamaJoinTournamentsContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> ListTournamentRecordsAsync(
            NakamaConnection connection,
            NakamaTournamentRecordsContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> ListTournamentRecordsAroundAsync(
            NakamaConnection connection,
            NakamaTournamentRecordsAroundContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> TournamentWriteAsync(
            NakamaConnection connection,
            NakamaTournamentWriteRecordContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> GetLeaderboardAsync(
            NakamaConnection connection,
            NakamaLeaderboardGetRecordsContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> GetLeaderboardAroundAsync(
            NakamaConnection connection,
            NakamaLeaderboardGetRecordsAroundContract contract,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> ExecuteRpcContractAsync(
            NakamaConnection connection,
            IRemoteMetaContract contract,
            CancellationToken cancellation = default);
        
        UniTask<NakamaServiceResult> AuthenticateAsync(
            INakamaAuthenticateData authenticateData,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> LoadUsersAsync(
            NakamaUsersContract usersContract,
            NakamaConnection connection,
            CancellationToken cancellation = default);

        UniTask<ContractMetaResult> LoadAccountAsync(
            NakamaConnection connection,
            CancellationToken cancellation = default);

        bool TryDequeue(out ContractMetaResult result);

        /// <summary>
        /// sign out from Namaka server and clear all authentication data
        /// </summary>
        UniTask<NakamaLogoutResult> SignOutAsync();

        UniTask<IApiAccount> GetUserProfileAsync();
        UniTask<NakamaServiceResult> ConnectToServerAsync(CancellationToken cancellation = default);
    }
}