namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using Nakama;
    using Newtonsoft.Json;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.DateTime;
    using UnityEngine;

    [Serializable]
    public class NakamaAuthenticate : INakamaAuthenticate
    {
	    private NakamaSettings _settings;
	    private LifeTime _lifeTime;
	    
	    public NakamaAuthenticate(NakamaSettings settings)
	    {
		    _lifeTime = new LifeTime();
		    _settings = settings;
	    }
	    
	    /// <summary>
	    ///  Authenticate user by custom data. Try to convert authdata into supported type.
	    /// </summary>
	    /// <param name="client"></param>
	    /// <param name="authenticateData"></param>
	    /// <returns>NakamaSessionResult</returns>
        public async UniTask<NakamaSessionResult> AuthenticateAsync(IClient client,
	        INakamaAuthenticateData authenticateData)
        {
			var restoreResult = await RestoreSessionAsync(authenticateData.AuthTypeName, client);
			if(restoreResult.success) return restoreResult;
			
	        ISession nakamaSession = null;
	        
	        if (authenticateData is NakamaIdAuthenticateData idData)
	        {
		        // get a new refresh token
		        nakamaSession = await client.AuthenticateDeviceAsync(idData.clientId,
			        idData.userName,
				        idData.create,idData.vars,
				        idData.retryConfiguration);
	        }

	        if (nakamaSession == null)
	        {
		        return new NakamaSessionResult()
		        {
			        session = null,
			        error = $"Cannot authenticate user by {authenticateData.AuthTypeName}",
			        success = false
		        };
	        }

	        var nakamaSessionData = new NakamaSessionData()
	        {
		        authToken = nakamaSession.AuthToken,
		        authType = authenticateData.AuthTypeName,
		        refreshToken = nakamaSession.RefreshToken,
		        timestamp = DateTime.Now.ToUnixTimestamp()
	        };

	        var prefsValue = JsonConvert.SerializeObject(nakamaSessionData);
	        
	        PlayerPrefs.SetString(NakamaConstants.NakamaAuthenticateKey,prefsValue);
	        
	        var result = new NakamaSessionResult()
	        {
		        session = nakamaSession,
		        error = string.Empty,
		        success = true,
	        };

	        return result;
        }
        
        public void Dispose() => _lifeTime.Terminate();

        public async UniTask<NakamaSessionResult> RestoreSessionAsync(string authType, IClient client)
        {
	        var result = new NakamaSessionResult()
	        {
		        session = null,
		        error = $"cannot restore session by {authType}",
		        success = false,
	        };
	        
	        var authData = PlayerPrefs.GetString(NakamaConstants.NakamaAuthenticateKey, string.Empty);

	        if (string.IsNullOrEmpty(authData)) return result;

	        var restoreData = JsonConvert.DeserializeObject<NakamaSessionData>(authData);
	        if (!restoreData.authType.Equals(authType))
		        return result;
	        
		    var session = await RestoreSessionAsync(restoreData.authToken, restoreData.refreshToken, client);
	        
	        if (session == null) return result;
	        
	        return new NakamaSessionResult()
	        {
		        session = session,
		        error = string.Empty,
		        success = true,
	        };
        }

        public async UniTask<ISession> RestoreSessionAsync(string authToken,string refreshToken, IClient client)
        {
	        var session = Session.Restore(authToken, refreshToken);
	        
	        // Check whether a session is close to expiry.
	        if (!session.HasExpired(DateTime.UtcNow.AddSeconds(_settings.refreshTokenInterval))) 
		        return session;
	        
	        try
	        {
		        // get a new access token
		        session = await client.SessionRefreshAsync(session,canceller:_lifeTime);
		        return session;
	        }
	        catch (ApiResponseException ex)
	        {
		        GameLog.LogError($"Nakama Restore Session Status {ex.StatusCode} GRPC {ex.GrpcStatusCode} Message {ex.Message}");	   
	        }

	        return null;
        }
        

    }

    [Serializable]
    public class NakamaSessionData
    {
	    public string authType;
	    public long timestamp;
	    public string authToken;
	    public string refreshToken;
    }

    [Serializable]
    public struct NakamaSessionResult
    {
	    public bool success;
	    public string error;
	    public ISession session;
    }
}