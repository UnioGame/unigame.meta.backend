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
        
        private LifeTime _lifeTime;
        private Subject<MetaContractCallData> _contractStream;
        private bool _resultReady = false;
        private ContractDataResult _lastResult;
        
        public ThrottleMetaContract()
            :this(DefaultInterval,DefaultType, TimeProvider.System) { }

        public ThrottleMetaContract(int delay,ThrottleType throttleType, TimeProvider time)
        {
            _lifeTime = new();
            intervalValue = delay;
            timeProvider = time;
            
            _contractStream = new Subject<MetaContractCallData>();
            _contractStream.AddTo(_lifeTime);

            var executionObservable = _contractStream.AsObservable();
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
                    .AttachExternalCancellation(x.CancellationToken).Forget())
                .AddTo(_lifeTime);
        }

        public async UniTask<ContractDataResult> ExecuteAsync(IRemoteMetaContract contract, CancellationToken cancellationToken)
        {
            if (intervalValue <= 0)
                return await contract.ExecuteAsync(cancellationToken);
            
            _contractStream.OnNext(new MetaContractCallData()
            {
                CancellationToken = cancellationToken,
                Contract = contract
            });
            
            await UniTask.WaitWhile(this,static x => x._resultReady == false, cancellationToken: cancellationToken);
            _resultReady = false;
            
            var result = _lastResult;
            return result;
        }

        public void Dispose() => _lifeTime.Terminate();

        public async UniTask<ContractDataResult> ExecuteAsync(MetaContractCallData data)
        {
            var result = await data.Contract.ExecuteAsync(data.CancellationToken);
            
            _lastResult = result;
            _resultReady = true;
            
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