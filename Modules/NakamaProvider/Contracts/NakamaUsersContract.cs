using System;
using System.Collections.Generic;
using Nakama;
using UniGame.MetaBackend.Runtime;

[Serializable]
public class NakamaUsersContract : NakamaContract<string,IApiUsers>
{
    public List<string> userIds = new();
    public List<string> userNames = new();
    public List<string> facebookIds = new();

    public IEnumerable<string> Ids => userIds;
    public IEnumerable<string> Usernames => userNames;
    private IEnumerable<string> FacebookIds => facebookIds;
    
    public override string Path => nameof(NakamaUsersContract);
}
