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
        public static TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(500);
        
        public TimeSpan interval = DefaultInterval;
        public TimeProvider timeProvider = TimeProvider.System;
        public LifeTime lifeTime;
        public DateTime lastExecutionTime;
        public IRemoteMetaContract delayedContract;
        public Subject<ContractDataResult> contractStream;

        public DebounceMetaContract():this(DefaultInterval, TimeProvider.System) { }

        public DebounceMetaContract(TimeSpan interval, TimeProvider timeProvider)
        {
            lifeTime = new();

            this.interval = interval;
            this.timeProvider = timeProvider;

            contractStream = new Subject<ContractDataResult>();
            contractStream.AddTo(lifeTime);
        }

        public Observable<ContractDataResult> ResultStream => contractStream;
        
        public async UniTask<ContractDataResult> ExecuteAsync(IRemoteMetaContract contract, CancellationToken cancellationToken = default)
        {
            var time = DateTime.UtcNow;
            var timeSinceLastExecution = time - lastExecutionTime;

            delayedContract = contract;

            if (interval > timeSinceLastExecution)
            {
                return await ExecuteAsync(interval, cancellationToken);
            }

            return await ExecuteAsync(TimeSpan.Zero, cancellationToken);
        }

        public void Dispose() => lifeTime.Terminate();

        private async UniTask<ContractDataResult> ExecuteAsync(TimeSpan delay,
            CancellationToken cancellationToken = default)
        {
            if (delay > TimeSpan.Zero)
                await UniTask.Delay(delay, true, cancellationToken: cancellationToken);

            if (delayedContract == null) return ContractDataResult.Empty;

            lastExecutionTime = DateTime.UtcNow;
            var result = await delayedContract.ExecuteAsync(cancellationToken);
            contractStream.OnNext(result);
            return result;
        }
    }
}