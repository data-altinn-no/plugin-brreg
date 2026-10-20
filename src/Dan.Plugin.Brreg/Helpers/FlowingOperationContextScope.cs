using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Dan.Plugin.Brreg.Helpers
{
    public sealed class FlowingOperationContextScope : IDisposable
    {
        private bool inflight;
        private bool disposed;

        private OperationContext thisContext;
        private OperationContext originalContext;

        public FlowingOperationContextScope(IContextChannel channel) : this(new OperationContext(channel))
        {
        }

        public FlowingOperationContextScope(OperationContext context)
        {
            originalContext = OperationContext.Current;
            OperationContext.Current = thisContext = context;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (inflight || OperationContext.Current != thisContext)
                    throw new InvalidOperationException();
                disposed = true;
                OperationContext.Current = originalContext;
                thisContext = null;
                originalContext = null;
            }
        }

        internal void BeforeAwait()
        {
            if (inflight)
            {
                return;
            }

            inflight = true;
            // leave _thisContext as the current context
        }

        internal void AfterAwait()
        {
            if (!inflight)
            {
                throw new InvalidOperationException();
            }

            inflight = false;
            // ignore the current context, restore _thisContext
            OperationContext.Current = thisContext;
        }
    }

    // ContinueOnScope extension
    public static class TaskExt
    {
        public static SimpleAwaiter<TResult> ContinueOnScope<TResult>(this Task<TResult> @this, FlowingOperationContextScope scope)
        {
            return new SimpleAwaiter<TResult>(@this, scope.BeforeAwait, scope.AfterAwait);
        }

        // awaiter
        public class SimpleAwaiter<TResult> :
            System.Runtime.CompilerServices.INotifyCompletion
        {
            private readonly Task<TResult> task;

            private readonly Action beforeAwait;
            private readonly Action afterAwait;

            public SimpleAwaiter(Task<TResult> task, Action beforeAwait, Action afterAwait)
            {
                this.task = task;
                this.beforeAwait = beforeAwait;
                this.afterAwait = afterAwait;
            }

            public SimpleAwaiter<TResult> GetAwaiter()
            {
                return this;
            }

            public bool IsCompleted
            {
                get
                {
                    // don't do anything if the task completed synchronously
                    // (we're on the same thread)
                    if (task.IsCompleted)
                    {
                        return true;
                    }

                    beforeAwait();
                    return false;
                }
            }

            public TResult GetResult()
            {
                return task.Result;
            }

            // INotifyCompletion
            public void OnCompleted(Action continuation)
            {
                task.ContinueWith(t =>
                {
                    afterAwait();
                    continuation();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                SynchronizationContext.Current != null ?
                    TaskScheduler.FromCurrentSynchronizationContext() :
                    TaskScheduler.Current);
            }
        }
    }
}
