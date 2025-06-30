namespace UniGame.MetaBackend.Runtime
{
    using Nakama;
    using R3;

    public interface INakamaConnection
    {
        ReadOnlyReactiveProperty<IClient> Client { get; }
        ReadOnlyReactiveProperty<ISession> Session { get; }
        ReadOnlyReactiveProperty<IApiAccount> Account { get; }
        ReadOnlyReactiveProperty<ISocket> Socket { get; }
        ReadOnlyReactiveProperty<string> Token { get; }
        ReadOnlyReactiveProperty<Shared.ConnectionState> State { get; }
        ReadOnlyReactiveProperty<INakamaAuthenticateData> AuthenticateData { get; }
        
        ReadOnlyReactiveProperty<NakamaServerData> ServerData { get; }
    }
}