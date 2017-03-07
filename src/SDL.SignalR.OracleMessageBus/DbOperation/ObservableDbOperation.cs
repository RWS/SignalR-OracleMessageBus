using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class ObservableDbOperation : DbOperation, IObservableDbOperation, IDbBehavior
    {
        #region [ private fields ]

        private static readonly TimeSpan DependencyTimeout = TimeSpan.FromSeconds(60);

        private readonly Tuple<int, int>[] _updateLoopRetryDelays =
        {
            Tuple.Create(0, 3),    // 0ms x 3
            Tuple.Create(10, 3),   // 10ms x 3
            Tuple.Create(50, 2),   // 50ms x 2
            Tuple.Create(100, 2),  // 100ms x 2
            Tuple.Create(200, 2),  // 200ms x 2
            Tuple.Create(1000, 2), // 1000ms x 2
            Tuple.Create(1500, 2), // 1500ms x 2
            Tuple.Create(3000, 1)  // 3000ms x 1
        };

        private readonly object _stopLocker = new object();
        private readonly ManualResetEventSlim _stopHandle = new ManualResetEventSlim(true);
        private volatile bool _disposing;
        private long _notificationState;

        private readonly bool _useOracleDependency;

        private readonly IDbBehavior _dbBehavior;
        private readonly IOracleDependencyManager _oracleDependencyManager;
        private readonly ISignalrDbDependencyFactory _signalrDbDependencyFactory;

        #endregion

        #region [ .ctors ]

        public ObservableDbOperation(string connectionString, string commandText, TraceSource traceSource,
            IDbBehavior dbBehavior,
            IDbProviderFactory dbProviderFactory,
            IOracleDependencyManager oracleDependencyManager,
            ISignalrDbDependencyFactory signalrDbDependencyFactory,
            bool useOracleDependency,
            params IDataParameter[] parameters)
            : base(connectionString, commandText, traceSource, dbProviderFactory, parameters)
        {
            _dbBehavior = dbBehavior ?? this;
            _oracleDependencyManager = oracleDependencyManager;
            _useOracleDependency = useOracleDependency;
            _signalrDbDependencyFactory = signalrDbDependencyFactory;
        }

        #endregion

        #region [ IObservableDbOperation implementation ]

        public event Action Queried;
        public event Action Changed;
        public event Action<Exception> Faulted;

        public void ExecuteReaderWithUpdates(Action<IDataRecord, IDbOperation> processRecord)
        {
            lock (_stopLocker)
            {
                if (_disposing)
                {
                    return;
                }
                _stopHandle.Reset();
            }

            bool useNotifications = _useOracleDependency;

            IList<Tuple<int, int>> delays = _dbBehavior.UpdateLoopRetryDelays;

            for (var i = 0; i < delays.Count; i++)
            {
                if (i == 0 && useNotifications)
                {
                    // Reset the state to ProcessingUpdates if this is the start of the loop.
                    // This should be safe to do here without Interlocked because the state is protected
                    // in the other two cases using Interlocked, i.e. there should only be one instance of
                    // this running at any point in time.
                    _notificationState = NotificationState.ProcessingUpdates;
                }

                Tuple<int, int> retry = delays[i];
                int retryDelay = retry.Item1;
                int retryCount = retry.Item2;

                for (int j = 0; j < retryCount; j++)
                {
                    if (_disposing)
                    {
                        Stop(null);
                        return;
                    }

                    if (retryDelay > 0)
                    {
                        Trace.TraceVerbose("Waiting {0} ms before checking for messages again", retryDelay);

                        Thread.Sleep(retryDelay);
                    }

                    var recordCount = 0;
                    try
                    {
                        recordCount = ExecuteReader(processRecord);

                        if (Queried != null)
                        {
                            Queried();
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error in Oracle receive loop: {0}", ex);

                        if (Faulted != null)
                        {
                            Faulted(ex);
                        }
                    }

                    if (recordCount > 0)
                    {
                        Trace.TraceVerbose("{0} records received", recordCount);

                        // We got records so start the retry loop again
                        i = -1;
                        break;
                    }

                    Trace.TraceVerbose("No records received");

                    var isLastRetry = i == delays.Count - 1 && j == retryCount - 1;

                    if (isLastRetry)
                    {
                        // Last retry loop iteration
                        if (!useNotifications)
                        {
                            // Last retry loop and we're not using notifications so just stay looping on the last retry delay
                            j = j - 1;
                        }
                        else
                        {
                            #region [ use notifications ]
                            // No records after all retries, set up a Oracle notification
                            try
                            {
                                Trace.TraceVerbose("Setting up Oracle notification");

                                try
                                {
                                    recordCount = ExecuteReader(processRecord, command =>
                                    {
                                        _dbBehavior.AddOracleDependency(command,
                                            e => OracleDependency_OnChange(e, processRecord));
                                    });
                                }
                                catch (OracleException ex)
                                {
                                    Trace.TraceError("Error while executing reader. Details: {0}", ex);

                                    if (ex.ErrorCode == 29972 ||// error creating change notification (not enough rights and stuff)
                                        (ex.Errors != null && ex.Errors.Count > 0 && ex.Errors[0].Number == 65131))  // the feature Continuous Query Notification is not supported in a pluggable database.
                                    {
                                        useNotifications = false;

                                        // Re-enter the loop on the last retry delay
                                        j = j - 1;
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }

                                if (Queried != null)
                                {
                                    Queried();
                                }

                                if (recordCount > 0)
                                {
                                    Trace.TraceVerbose("Records were returned by the command that sets up the SQL notification, restarting the receive loop");

                                    i = -1;
                                    break; // break the inner for loop
                                }

                                var previousState = Interlocked.CompareExchange(ref _notificationState, NotificationState.AwaitingNotification,
                                    NotificationState.ProcessingUpdates);

                                if (previousState == NotificationState.AwaitingNotification)
                                {
                                    Trace.TraceError("A Oracle notification was already running. Overlapping receive loops detected, this should never happen. BUG!");

                                    return;
                                }

                                if (previousState == NotificationState.NotificationReceived)
                                {
                                    // Failed to change _notificationState from ProcessingUpdates to AwaitingNotification, it was already NotificationReceived

                                    Trace.TraceVerbose("The Oracle notification fired before the receive loop returned, restarting the receive loop");

                                    i = -1;
                                    break; // break the inner for loop
                                }

                                Trace.TraceVerbose("No records received while setting up Oracle notification");

                                // We're in a wait state for a notification now so check if we're disposing
                                lock (_stopLocker)
                                {
                                    if (_disposing)
                                    {
                                        _stopHandle.Set();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Error in SQL receive loop: {0}", ex);
                                if (Faulted != null)
                                {
                                    Faulted(ex);
                                }

                                // Re-enter the loop on the last retry delay
                                j = j - 1;

                                if (retryDelay > 0)
                                {
                                    Trace.TraceVerbose("Waiting {0} ms before checking for messages again", retryDelay);

                                    Thread.Sleep(retryDelay);
                                }
                            }

                            #endregion
                        }
                    }
                }
            }

            Trace.TraceVerbose("GetLastPayloadId loop exiting");
        }

        public void Dispose()
        {
            lock (_stopLocker)
            {
                _disposing = true;
            }

            if (_notificationState != NotificationState.Disabled)
            {
                try
                {
                    _oracleDependencyManager.RemoveRegistration(ConnectionString);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to stop oracle dependency: {0}", ex);
                }
            }

            if (Interlocked.Read(ref _notificationState) == NotificationState.ProcessingUpdates)
            {
                _stopHandle.Wait();
            }

            _stopHandle.Dispose();
        }

        #endregion

        #region [ IDbBehavior implementation ]

        public void AddOracleDependency(IDbCommand command, Action<ISignalRDbNotificationEventArgs> callback)
        {
            ISignalRDbDependency dependency = _signalrDbDependencyFactory.CreateDbDependency(command, true, (long)DependencyTimeout.TotalSeconds, false);
            dependency.OnChanged += (o, e) => callback(e);
            _oracleDependencyManager.RegisterDependency(dependency);
        }

        IList<Tuple<int, int>> IDbBehavior.UpdateLoopRetryDelays
        {
            get { return _updateLoopRetryDelays; }
        }

        #endregion

        #region [ helpers ]

        protected virtual void Stop(Exception ex)
        {
            if (ex != null)
            {
                if (Faulted != null)
                {
                    Faulted(ex);
                }
            }

            if (_notificationState != NotificationState.Disabled)
            {
                try
                {
                    Trace.TraceVerbose("Stopping Oracle notification listener");
                    _oracleDependencyManager.RemoveRegistration(ConnectionString);
                    Trace.TraceVerbose("Oracle notification listener stopped");
                }
                catch (Exception stopEx)
                {
                    Trace.TraceError("Error occurred while stopping Oracle notification listener: {0}", stopEx);
                }
            }

            lock (_stopLocker)
            {
                if (_disposing)
                {
                    _stopHandle.Set();
                }
            }
        }

        protected virtual void OracleDependency_OnChange(ISignalRDbNotificationEventArgs e,
            Action<IDataRecord, DbOperation> processRecord)
        {
            Trace.TraceInformation("Oracle notification change fired");

            lock (_stopLocker)
            {
                if (_disposing)
                {
                    return;
                }
            }

            var previousState = Interlocked.CompareExchange(ref _notificationState,
                NotificationState.NotificationReceived, NotificationState.ProcessingUpdates);

            if (previousState == NotificationState.NotificationReceived)
            {
                Trace.TraceError("Overlapping Oracle change notifications received, this should never happen, BUG!");

                return;
            }
            if (previousState == NotificationState.ProcessingUpdates)
            {
                // We're still in the original receive loop

                // New updates will be retrieved by the original reader thread
                Trace.TraceVerbose("Original reader processing is still in progress and will pick up the changes");

                return;
            }

            // _notificationState wasn't ProcessingUpdates (likely AwaitingNotification)
            // Check notification args for issues
            if ((OracleNotificationType) e.NotificationType == OracleNotificationType.Change)
            {
                if ((OracleNotificationInfo) e.NotificationInfo == OracleNotificationInfo.Update)
                {
                    Trace.TraceVerbose("Oracle notification details: Type={0}, Source={1}, Info={2}",
                        e.NotificationType, e.NotificationSource, e.NotificationInfo);
                }
                else if ((OracleNotificationInfo) e.NotificationInfo == OracleNotificationInfo.End)
                {
                    Trace.TraceVerbose("Oracle notification timed out");
                }
                else
                {
                    Trace.TraceError("Unexpected Oracle notification details: Type={0}, Source={1}, Info={2}",
                        e.NotificationType, e.NotificationSource, e.NotificationInfo);

                    if (Faulted != null)
                    {
                        Faulted(new OracleMessageBusException(string.Format(CultureInfo.InvariantCulture,
                            "An unexpected SqlNotificationType was received. Details: Type={0}, Source={1}, Info={2}",
                            e.NotificationType, e.NotificationSource, e.NotificationInfo)));
                    }
                }
            }
            else if ((OracleNotificationType) e.NotificationType == OracleNotificationType.Subscribe)
            {
                Trace.TraceError("Oracle notification subscription error: Type={0}, Source={1}, Info={2}",
                    e.NotificationType, e.NotificationSource, e.NotificationInfo);


                // Unknown subscription error, let's stop using query notifications
                _notificationState = NotificationState.Disabled;
                _oracleDependencyManager.RemoveRegistration(ConnectionString);
            }

            if (Changed != null)
            {
                Changed();
            }
        }

        #endregion
    }
}