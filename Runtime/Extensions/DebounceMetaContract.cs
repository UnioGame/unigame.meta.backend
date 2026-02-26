namespace Extensions
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using R3;
    using UniGame.MetaBackend.Runtime;
    using UniGame.MetaBackend.Shared;
    using UniGame.Runtime.DataFlow;

    public class DebounceMetaContract : IDisposable
    {
        public static TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(200);
        
        public TimeSpan interval = DefaultInterval;
        public TimeProvider timeProvider = TimeProvider.System;
        public LifeTime lifeTime;
        public Subject<ContractDataResult> contractStream;
        public Subject<DebounceContractData> contractExecutionStream;

        public DebounceMetaContract():this(DefaultInterval, TimeProvider.System) { }

        public DebounceMetaContract(TimeSpan interval, TimeProvider timeProvider)
        {
            lifeTime = new();

            this.interval = interval;
            this.timeProvider = timeProvider;

            contractExecutionStream = new Subject<DebounceContractData>();
            contractExecutionStream.AddTo(lifeTime);
            
            contractStream = new Subject<ContractDataResult>();
            contractStream.AddTo(lifeTime);

            contractExecutionStream
                .Debounce(interval, timeProvider)
                .Subscribe(this,static (x,y) => y.ExecuteAsync(x)
                    .AttachExternalCancellation(x.CancellationToken)
                    .Forget())
                .AddTo(lifeTime);
        }

        public Observable<ContractDataResult> ResultStream => contractStream;
        
        public async UniTask<ContractDataResult> ExecuteAsync(IRemoteMetaContract contract, CancellationToken cancellationToken)
        {
            if (interval <= TimeSpan.Zero)
                return await contract.ExecuteAsync(cancellationToken);
            
            contractExecutionStream.OnNext(new DebounceContractData()
            {
                CancellationToken = cancellationToken,
                Contract = contract
            });
            
            var result = await contractStream.FirstAsync(cancellationToken);
            return result;
        }

        public void Dispose() => lifeTime.Terminate();

        public async UniTask<ContractDataResult> ExecuteAsync(DebounceContractData data)
        {
            await UniTask.SwitchToMainThread();
            var result = await data.Contract.ExecuteAsync(data.CancellationToken);
            contractStream.OnNext(result);
            return result;
        }
    }
    
    public struct DebounceContractData
    {
        public IRemoteMetaContract Contract;
        public CancellationToken CancellationToken;
    }
}