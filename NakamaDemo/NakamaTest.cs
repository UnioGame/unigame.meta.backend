using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;
using Game.Nakama.Models;
using MetaService.Runtime;
using Nakama;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UniCore.Runtime.ProfilerTools;
using UniGame.Context.Runtime;
using UniGame.MetaBackend.Runtime;
using UniGame.MetaBackend.Shared;
using UniGame.Runtime.Rx.Extensions;
using UniGame.Runtime.Utils;

using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

public class NakamaTest : MonoBehaviour
{
    public bool autoSignIn = true;
    public BackendMetaSource source;

    [SerializeReference]
    [InlineButton(nameof(UpdateContracts))]
    public List<INakamaContract> contracts = new();
    
    [ShowIf(nameof(IsPlaying))]
    [ListDrawerSettings(ListElementLabelName = "name")]
    public List<NakamaDemoContract> demoContracts = new();
    
    private EntityContext _context;
    private INakamaService _nakamaService;
    private IBackendMetaService _backendMetaService;
    
    public bool IsPlaying => Application.isPlaying;
    
    private void Start()
    {
        InitializeAsync().Forget();
    }

    private void OnDestroy()
    {
        _context.Dispose();
    }

    [Button]
    public void IdAuth()
    {
        IdAuthAsync().Forget();
    }

    [Button]
    public void CallAllRpc()
    {
        CallAllRpcAsync().Forget();
    }
    
    [Button]
    public void GetAccount()
    {
        GetAccountAsync().Forget();
    }
    
    
    public void GetUsers()
    {
        
    }

    public async UniTask CallAllRpcAsync()
    {
        await IdAuthAsync();
        foreach (var rpcName in contracts)
        {
            await ExecuteContractAsync(rpcName);
        }
    }

    public void ExecuteContract(INakamaContract contract)
    {
        
        ExecuteContractAsync(contract).Forget();
    }
    
    public async UniTask ExecuteContractAsync(INakamaContract contract)
    {
        var r = await contract.ExecuteAsync<NakamaModel<LevelModel>>();
        
        if (_nakamaService == null) return;
        
        var result = await _backendMetaService.ExecuteAsync(contract);
        var data = result.result;
        var color = result.success ? Color.green : Color.red;

        var stringData = string.Empty;
        if(data !=null)
            stringData = data as string ?? JsonConvert.SerializeObject(data);
        
        GameLog.Log($"Nakama RPC Call: {contract.Path} | Payload {contract.Payload} | Success: {result.success} DATA: \n{stringData}" , color);
    }
    
    public async UniTask GetAccountAsync()
    {
        var accountContract = new NakamaAccountContract();
        var accountResult = await accountContract.ExecuteAsync<IApiAccount>();
        
        if (accountResult.success == false)
        {
            Debug.LogError("Failed to get account: " + accountResult.error);
            return;
        }

        var account = accountResult.data;
        var userApi = account.User;
        
        GameLog.Log($"Account ID: {account.CustomId} | {userApi.Id} | {account.Email}", Color.green);
    }

    public async UniTask SignInAsync()
    {
        await IdAuthAsync();
    }
    
    public async UniTask IdAuthAsync()
    {
        if (_nakamaService == null)
        {
            Debug.LogError("Nakama service is not initialized.");
            return;
        }

        var clientId = SystemInfo.deviceUniqueIdentifier;
        
        var idAuth = new NakamaIdAuthenticateData()
        {
            clientId = clientId,
            userName = "Demo User",
            create = true,
            vars = null,
            retryConfiguration = null,
        };

        var authResult  = await _nakamaService.SignInAsync(idAuth);
        
        if (authResult.success == false)
        {
            Debug.LogError("Nakama ID authentication failed: " + authResult.error);
            return;
        }
        
        GameLog.Log($"Nakama ID {authResult.userId} authentication : {authResult.success}" ,Color.green);
        
    }

    private async UniTask InitializeAsync()
    {
        _context = new EntityContext();
        
        var sourceInstance = Instantiate(source);
        await sourceInstance.RegisterAsync(_context);

        _backendMetaService = _context.Get<IBackendMetaService>();
        
        _nakamaService = await _context
            .Receive<INakamaService>()
            .FirstNotNull()
            .ReceiveFirstAsync();

        if (_nakamaService == null)
        {
            Debug.LogError($"Nakama service not found in context.");
        }
        
        demoContracts.Clear();

        foreach (var contract in contracts)
        {
            demoContracts.Add(new NakamaDemoContract(contract, ExecuteContract));
        }

        if (autoSignIn)
            await SignInAsync();
    }

    public void UpdateContracts()
    {
#if UNITY_EDITOR
    
        contracts.Clear();
        var types = TypeCache.GetTypesDerivedFrom<INakamaContract>();
        foreach (var type in types)
        {
            if(type.IsAbstract) continue;
            if(type.IsInterface) continue;

            var contract = type.CreateWithDefaultConstructor();
            if (contract is INakamaContract nakamaContract)
            {
                contracts.Add(nakamaContract);
            }
        }

#endif
    }

}

[Serializable]
public class NakamaDemoContract
{
    private readonly Action<INakamaContract> _executor;

    public string name;

    [SerializeReference]
    public INakamaContract contract;

    public NakamaDemoContract(INakamaContract value,Action<INakamaContract> executor)
    {
        contract = value;
        name = value.Path;
        _executor = executor;
    }
        
    [Button]
    public void Execute()
    {
        _executor?.Invoke(contract);
    }
}
