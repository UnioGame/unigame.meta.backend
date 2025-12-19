using UniGame.MetaBackend.Runtime;
using UniGame.MetaBackend.Shared;

public interface INakamaAuthContract : INakamaContract
{
    INakamaAuthenticateData AuthData { get; }
}