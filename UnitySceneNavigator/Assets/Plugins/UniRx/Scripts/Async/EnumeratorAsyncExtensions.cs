﻿#if CSHARP_7_OR_LATER
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace UniRx.Async
{
    public static class EnumeratorAsyncExtensions
    {
        public static UniTask.Awaiter GetAwaiter(this IEnumerator enumerator)
        {
            return enumerator.ConfigureAwait().GetAwaiter();
        }

        public static UniTask ToUniTask(this IEnumerator enumerator)
        {
            return enumerator.ConfigureAwait();
        }

        public static UniTask ConfigureAwait(this IEnumerator enumerator, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default(CancellationToken))
        {
            var awaiter = new EnumeratorAwaiter(enumerator, cancellationToken);
            PlayerLoopHelper.AddAction(timing, awaiter);
            return new UniTask(awaiter);
        }

        class EnumeratorAwaiter : IAwaiter, IPlayerLoopItem
        {
            const int Unfinished = 0;
            const int Success = 1;
            const int Error = 2;
            const int Canceled = 3;

            IEnumerator innerEnumerator;
            CancellationToken cancellationToken;
            Action continuation;
            int completeState;
            ExceptionDispatchInfo exception;

            public EnumeratorAwaiter(IEnumerator innerEnumerator, CancellationToken cancellationToken)
            {
                this.innerEnumerator = innerEnumerator;
                this.continuation = null;
                this.completeState = Unfinished;
                this.cancellationToken = cancellationToken;
            }

            public bool IsCompleted
            {
                get
                {
                    return cancellationToken.IsCancellationRequested || (completeState != Unfinished);
                }
            }

            public AwaiterStatus Status => (AwaiterStatus)completeState;

            public void GetResult()
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (completeState == Success)
                {
                    return;
                }
                else if (completeState == Error)
                {
                    exception.Throw();
                }
                else if (completeState == Canceled)
                {
                    throw new OperationCanceledException();
                }
            }

            public bool MoveNext()
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completeState = Canceled;
                    continuation?.Invoke();
                    return false;
                }

                try
                {
                    if (innerEnumerator.MoveNext())
                    {
                        return true;
                    }
                    else
                    {
                        completeState = Success;
                    }
                }
                catch (Exception ex)
                {
                    exception = ExceptionDispatchInfo.Capture(ex);
                    completeState = Error;
                }

                continuation?.Invoke();
                return false;
            }

            public void OnCompleted(Action continuation)
            {
                this.continuation = continuation;
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                this.continuation = continuation;
            }
        }
    }
}

#endif