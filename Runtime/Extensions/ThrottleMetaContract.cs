namespace Extensions
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using R3;
    using UniGame.MetaBackend.Runtime;
    using UniGame.MetaBackend.Shared;
    using UniGame.Runtime.DataFlow;

    public class ThrottleMetaContract : IDisposable
    {
        public readonly static int DefaultInterval = 500;
        public readonly static ThrottleType DefaultType =  ThrottleType.ThrottleFirstLast;
        
        public int intervalValue;
        public TimeProvider timeProvider = TimeProvider.System;
        public LifeTime lifeTime;
        public Subject<ContractDataResult> contractStream;
        public Subject<MetaContractCallData> contractExecutionStream;
        
        public ThrottleMetaContract()
            :this(DefaultInterval,DefaultType, TimeProvider.System) { }

        public ThrottleMetaContract(int delay,ThrottleType throttleType, TimeProvider time)
        {
            lifeTime = new();
            intervalValue = delay;
            timeProvider = time;
            
            contractExecutionStream = new Subject<MetaContractCallData>();
            contractExecutionStream.AddTo(lifeTime);
            
            contractStream = new Subject<ContractDataResult>();
            contractStream.AddTo(lifeTime);

            var executionObservable = contractExecutionStream.AsObservable();
            var interval = TimeSpan.FromMilliseconds(delay);
            
            switch (throttleType)
            {
                case ThrottleType.ThrottleFirst:
                    executionObservable = executionObservable.ThrottleFirst(interval, timeProvider);
                    break;
                case ThrottleType.ThrottleLast:
                    executionObservable = executionObservable.ThrottleLast(interval, timeProvider);
                    break;
                case ThrottleType.ThrottleFirstLast:
                    executionObservable = executionObservable.ThrottleFirstLast(interval, timeProvider);
                    break;
                case ThrottleType.ThrottleFirstFrame:
                    executionObservable = executionObservable.ThrottleFirstFrame(delay);
                    break;
                case ThrottleType.ThrottleLastFrame:
                    executionObservable = executionObservable.ThrottleLastFrame(delay);
                    break;
                case ThrottleType.ThrottleFirstLastFrame:
                    executionObservable = executionObservable.ThrottleFirstLastFrame(delay);
                    break;
                default:
                    executionObservable = executionObservable.ThrottleFirstLast(interval, timeProvider);
                    break;
            }

            executionObservable
                .Subscribe(this,static (x,y) => y.ExecuteAsync(x)
                    .AttachExternalCancellation(x.CancellationToken)
                    .Forget())
                .AddTo(lifeTime);
        }

        public async UniTask<ContractDataResult> ExecuteAsync(IRemoteMetaContract contract, CancellationToken cancellationToken)
        {
            if (intervalValue <= 0)
                return await contract.ExecuteAsync(cancellationToken);
            
            contractExecutionStream.OnNext(new MetaContractCallData()
            {
                CancellationToken = cancellationToken,
                Contract = contract
            });
            
            var result = await contractStream.FirstAsync(cancellationToken);
            return result;
        }

        public void Dispose() => lifeTime.Terminate();

        public async UniTask<ContractDataResult> ExecuteAsync(MetaContractCallData data)
        {
            await UniTask.SwitchToMainThread();
            var result = await data.Contract.ExecuteAsync(data.CancellationToken);
            contractStream.OnNext(result);
            return result;
        }
    }

    public enum ThrottleType
    {
        ThrottleFirst,
        ThrottleLast,
        ThrottleFirstLast,
        ThrottleFirstLastFrame,
        ThrottleFirstFrame,
        ThrottleLastFrame,
    }
}