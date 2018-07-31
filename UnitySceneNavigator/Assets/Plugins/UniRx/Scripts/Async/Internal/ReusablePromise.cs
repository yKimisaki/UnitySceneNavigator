﻿#if CSHARP_7_OR_LATER
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Runtime.ExceptionServices;

namespace UniRx.Async.Internal
{
    // 'public', user can use this(but be careful).

    public class ReusablePromise : IAwaiter
    {
        ExceptionDispatchInfo exception;
        object continuation; // Action or Queue<Action>
        AwaiterStatus status;

        public UniTask Task => new UniTask(this);

        public virtual bool IsCompleted
        {
            get
            {
                if ((status == AwaiterStatus.Canceled) || (status == AwaiterStatus.Faulted)) return true;
                return false;
            }
        }

        public virtual void GetResult()
        {
            switch (status)
            {
                case AwaiterStatus.Succeeded:
                    return;
                case AwaiterStatus.Faulted:
                    exception.Throw();
                    break;
                case AwaiterStatus.Canceled:
                    throw new OperationCanceledException();
                default:
                    break;
            }

            throw new InvalidOperationException("Invalid Status:" + status);
        }

        public AwaiterStatus Status => status;

        void IAwaiter.GetResult()
        {
            GetResult();
        }

        public void ResetStatus()
        {
            status = AwaiterStatus.Pending;
        }

        public bool TrySetCanceled()
        {
            if (status == AwaiterStatus.Pending)
            {
                status = AwaiterStatus.Canceled;
                TryInvokeContinuation();
                return true;
            }
            return false;
        }

        public bool TrySetException(Exception ex)
        {
            if (status == AwaiterStatus.Pending)
            {
                status = AwaiterStatus.Faulted;
                exception = ExceptionDispatchInfo.Capture(ex);
                TryInvokeContinuation();
                return true;
            }
            return false;
        }

        public bool TrySetResult()
        {
            if (status == AwaiterStatus.Pending)
            {
                status = AwaiterStatus.Succeeded;
                TryInvokeContinuation();
                return true;
            }
            return false;
        }

        void TryInvokeContinuation()
        {
            if (continuation == null) return;

            if (continuation is Action act)
            {
                continuation = null;
                act();
            }
            else
            {
                // reuse Queue(don't null clear)
                var q = (MinimumQueue<Action>)continuation;
                var size = q.Count;
                for (int i = 0; i < size; i++)
                {
                    q.Dequeue().Invoke();
                }
            }
        }

        public void OnCompleted(Action action)
        {
            UnsafeOnCompleted(action);
        }

        public void UnsafeOnCompleted(Action action)
        {
            if (continuation == null)
            {
                continuation = action;
                return;
            }
            else
            {
                if (continuation is Action act)
                {
                    var q = new MinimumQueue<Action>(4);
                    q.Enqueue(act);
                    q.Enqueue(action);
                    continuation = q;
                    return;
                }
                else
                {
                    ((MinimumQueue<Action>)continuation).Enqueue(action);
                }
            }
        }
    }

    public class ReusablePromise<T> : IAwaiter<T>
    {
        T result;
        ExceptionDispatchInfo exception;
        object continuation; // Action or Queue<Action>
        AwaiterStatus status;

        public UniTask<T> Task => new UniTask<T>(this);

        public virtual bool IsCompleted
        {
            get
            {
                if ((status == AwaiterStatus.Canceled) || (status == AwaiterStatus.Faulted)) return true;
                return false;
            }
        }

        public virtual T GetResult()
        {
            switch (status)
            {
                case AwaiterStatus.Succeeded:
                    return result;
                case AwaiterStatus.Faulted:
                    exception.Throw();
                    break;
                case AwaiterStatus.Canceled:
                    throw new OperationCanceledException();
                default:
                    break;
            }

            throw new InvalidOperationException("Invalid Status:" + status);
        }

        public AwaiterStatus Status => status;

        void IAwaiter.GetResult()
        {
            GetResult();
        }

        public void ResetStatus()
        {
            status = AwaiterStatus.Pending;
        }

        public bool TrySetCanceled()
        {
            if (status == AwaiterStatus.Pending)
            {
                status = AwaiterStatus.Canceled;
                TryInvokeContinuation();
                return true;
            }
            return false;
        }

        public bool TrySetException(Exception ex)
        {
            if (status == AwaiterStatus.Pending)
            {
                status = AwaiterStatus.Faulted;
                exception = ExceptionDispatchInfo.Capture(ex);
                TryInvokeContinuation();
                return true;
            }
            return false;
        }

        public bool TrySetResult(T result)
        {
            if (status == AwaiterStatus.Pending)
            {
                status = AwaiterStatus.Succeeded;
                this.result = result;
                TryInvokeContinuation();
                return true;
            }
            return false;
        }

        void TryInvokeContinuation()
        {
            if (continuation == null) return;

            if (continuation is Action act)
            {
                continuation = null;
                act();
            }
            else
            {
                // reuse Queue(don't null clear)
                var q = (MinimumQueue<Action>)continuation;
                var size = q.Count;
                for (int i = 0; i < size; i++)
                {
                    q.Dequeue().Invoke();
                }
            }
        }

        public void OnCompleted(Action action)
        {
            UnsafeOnCompleted(action);
        }

        public void UnsafeOnCompleted(Action action)
        {
            if (continuation == null)
            {
                continuation = action;
                return;
            }
            else
            {
                if (continuation is Action act)
                {
                    var q = new MinimumQueue<Action>(4);
                    q.Enqueue(act);
                    q.Enqueue(action);
                    continuation = q;
                    return;
                }
                else
                {
                    ((MinimumQueue<Action>)continuation).Enqueue(action);
                }
            }
        }
    }
}

#endif