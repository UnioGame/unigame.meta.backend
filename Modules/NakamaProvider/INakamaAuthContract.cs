using UniGame.MetaBackend.Runtime;

public interface INakamaAuthContract : INakamaContract
{
    INakamaAuthenticateData AuthData { get; }
}