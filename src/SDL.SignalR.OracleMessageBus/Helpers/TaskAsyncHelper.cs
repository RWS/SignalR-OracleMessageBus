using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Sdl.SignalR.OracleMessageBus
{
    internal static class TaskAsyncHelper
    {
        private static readonly Task EmptyTask = MakeTask<object>(null);

        private static Task<T> MakeTask<T>(T value)
        {
            return FromResult(value);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Empty
        {
            get
            {
                return EmptyTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static TTask Catch<TTask>(this TTask task, Action<AggregateException, object> handler, object state, TraceSource traceSource = null) where TTask : Task
        {
            if (task != null && task.Status != TaskStatus.RanToCompletion)
            {
                if (task.Status == TaskStatus.Faulted)
                {
                    ExecuteOnFaulted(handler, state, task.Exception, traceSource);
                }
                else
                {
                    AttachFaultedContinuation(task, handler, state, traceSource);
                }
            }

            return task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        private static void AttachFaultedContinuation<TTask>(TTask task, Action<AggregateException, object> handler, object state, TraceSource traceSource) where TTask : Task
        {
            task.ContinueWithPreservedCulture(innerTask => ExecuteOnFaulted(handler, state, innerTask.Exception, traceSource),
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        private static void ExecuteOnFaulted(Action<AggregateException, object> handler, object state, AggregateException exception, TraceSource traceSource)
        {
            // Observe Exception
            if (traceSource != null)
            {
                traceSource.TraceEvent(TraceEventType.Warning, 0, "Exception thrown by Task: {0}", exception);
            }
            handler(exception, state);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",
            Justification = "This is a shared file")]
        public static TTask Catch<TTask>(this TTask task, Action<AggregateException> handler,
            TraceSource traceSource = null) where TTask : Task
        {
            return task.Catch((ex, state) => ((Action<AggregateException>) state).Invoke(ex),
                handler,
                traceSource
                );
        }

        // Then extensions
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then(this Task task, Action successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return RunTask(task, successor);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<TResult>(this Task<TResult> task, Action<TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                case TaskStatus.Canceled:
                    return task;

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<TResult, object>.RunTask(task, successor);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed to the caller")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Finally(this Task task, Action<object> next, object state)
        {
            try
            {
                switch (task.Status)
                {
                    case TaskStatus.Faulted:
                    case TaskStatus.Canceled:
                        next(state);
                        return task;
                    case TaskStatus.RanToCompletion:
                        return FromMethod(next, state);

                    default:
                        return RunTaskSynchronously(task, next, state, onlyOnSuccess: false);
                }
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod(Action func)
        {
            try
            {
                func();
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod<T1>(Action<T1> func, T1 arg)
        {
            try
            {
                func(arg);
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static Task FromError(Exception e)
        {
            return FromError<object>(e);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static Task<T> FromError<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetUnwrappedException(e);
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static void SetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e)
        {
            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                tcs.SetException(aggregateException.InnerExceptions);
            }
            else
            {
                tcs.SetException(e);
            }
        }

        internal struct CulturePair
        {
            public CultureInfo Culture;
            public CultureInfo UICulture;
        }

        internal static CulturePair SaveCulture()
        {
            return new CulturePair
            {
                Culture = Thread.CurrentThread.CurrentCulture,
                UICulture = Thread.CurrentThread.CurrentUICulture
            };
        }

        internal static TResult RunWithPreservedCulture<T1, T2, TResult>(CulturePair preservedCulture, Func<T1, T2, TResult> func, T1 arg1, T2 arg2)
        {
            var replacedCulture = SaveCulture();
            try
            {
                Thread.CurrentThread.CurrentCulture = preservedCulture.Culture;
                Thread.CurrentThread.CurrentUICulture = preservedCulture.UICulture;
                return func(arg1, arg2);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = replacedCulture.Culture;
                Thread.CurrentThread.CurrentUICulture = replacedCulture.UICulture;
            }
        }

        internal static void RunWithPreservedCulture<T>(CulturePair preservedCulture, Action<T> action, T arg)
        {
            RunWithPreservedCulture(preservedCulture, (f, state) =>
            {
                f(state);
                return (object)null;
            },
            action, arg);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static void RunWithPreservedCulture(CulturePair preservedCulture, Action action)
        {
            RunWithPreservedCulture(preservedCulture, f => f(), action);
        }

        internal static Task ContinueWithPreservedCulture(this Task task, Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
        {
            var preservedCulture = SaveCulture();
            return task.ContinueWith(t => RunWithPreservedCulture(preservedCulture, continuationAction, t), continuationOptions);
        }

        internal static Task ContinueWithPreservedCulture<T>(this Task<T> task, Action<Task<T>> continuationAction, TaskContinuationOptions continuationOptions)
        {
            var preservedCulture = SaveCulture();
            return task.ContinueWith(t => RunWithPreservedCulture(preservedCulture, continuationAction, t), continuationOptions);
        }

        internal static Task ContinueWithPreservedCulture(this Task task, Action<Task> continuationAction)
        {
            return task.ContinueWithPreservedCulture(continuationAction, TaskContinuationOptions.None);
        }

        internal static Task ContinueWithPreservedCulture<T>(this Task<T> task, Action<Task<T>> continuationAction)
        {
            return task.ContinueWithPreservedCulture(continuationAction, TaskContinuationOptions.None);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        private static Task RunTask(Task task, Action successor)
        {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWithPreservedCulture(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetUnwrappedException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    try
                    {
                        successor();
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetUnwrappedException(ex);
                    }
                }
            });

            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        private static Task RunTaskSynchronously(Task task, Action<object> next, object state, bool onlyOnSuccess = true)
        {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWithPreservedCulture(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        if (!onlyOnSuccess)
                        {
                            next(state);
                        }

                        tcs.SetUnwrappedException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        if (!onlyOnSuccess)
                        {
                            next(state);
                        }

                        tcs.SetCanceled();
                    }
                    else
                    {
                        next(state);
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetUnwrappedException(ex);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        private static class TaskRunners<T, TResult>
        {
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
            internal static Task RunTask(Task<T> task, Action<T> successor)
            {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWithPreservedCulture(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetUnwrappedException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            successor(t.Result);
                            tcs.SetResult(null);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetUnwrappedException(ex);
                        }
                    }
                });

                return tcs.Task;
            }
        }
    }
}
