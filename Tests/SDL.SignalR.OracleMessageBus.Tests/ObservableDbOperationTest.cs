using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus.Tests
{
    [TestClass]
    public class ObservableDbOperationTest
    {
        private Tuple<int, int>[] _updateLoopRetryDelays =
        {
            Tuple.Create(0, 3),    // 0ms x 3
            Tuple.Create(10, 3),   // 10ms x 3
        };

        [TestMethod]
        public void Basic_Success()
        {
            var fakeDbBehavior = A.Fake<IDbBehavior>();
            A.CallTo(() => fakeDbBehavior.UpdateLoopRetryDelays).Returns(_updateLoopRetryDelays);

            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty, new TraceSource("ss"),
                fakeDbBehavior,
                A.Fake<IDbProviderFactory>(),
                A.Fake<IOracleDependencyManager>(),
                A.Fake<ISignalrDbDependencyFactory>(),
                true);

            dbOperation.ExecuteReaderWithUpdates(A.Fake<Action<IDataRecord, IDbOperation>>());
        }

        [TestMethod]
        public void CreateDependency_Success()
        {
            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty, new TraceSource("ss"),
                null,
                A.Fake<IDbProviderFactory>(),
                A.Fake<IOracleDependencyManager>(),
                A.Fake<ISignalrDbDependencyFactory>(),
                true);

            dbOperation.ExecuteReaderWithUpdates(A.Fake<Action<IDataRecord, IDbOperation>>());
        }

        [TestMethod]
        public void Pooling_Success()
        {
            var fakeDbReader = A.Fake<IDataReader>();
            var fakeDbReaderReadCall = A.CallTo(() => fakeDbReader.Read());
            fakeDbReaderReadCall.ReturnsNextFromSequence(true, true, false);

            var fakeEmptyDbReader = A.Fake<IDataReader>();
            A.CallTo(() => fakeEmptyDbReader.Read()).Returns(false);

            var fakeDbCommand = A.Fake<IDbCommand>();
            A.CallTo(() => fakeDbCommand.ExecuteReader()).ReturnsNextFromSequence(fakeDbReader, fakeEmptyDbReader);

            var fakeDbConnection = A.Fake<IDbConnection>();
            A.CallTo(() => fakeDbConnection.CreateCommand()).Returns(fakeDbCommand);
            var fakeDbConnectionDisposeCall = A.CallTo(() => fakeDbConnection.Dispose());

            var fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            A.CallTo(() => fakeDbProviderFactory.CreateConnection()).Returns(fakeDbConnection);

            var fakeProcessRecordAction = A.Fake<Action<IDataRecord, IDbOperation>>();
            var fakeProcessRecordActionInvokeCall = A.CallTo(() => fakeProcessRecordAction.Invoke(null, null)).WithAnyArguments();

            var fakeDbBehavior = A.Fake<IDbBehavior>();
            A.CallTo(() => fakeDbBehavior.UpdateLoopRetryDelays).Returns(_updateLoopRetryDelays);

            var fakeOracleDependencyManager = A.Fake<IOracleDependencyManager>();
            var fakeOracleDependencyManagerRegistryDepCall = A.CallTo(() => fakeOracleDependencyManager.RegisterDependency(null)).WithAnyArguments();

            var fakeOracleDependencyManagerRemoveRegistrationCall =
                A.CallTo(() => fakeOracleDependencyManager.RemoveRegistration(string.Empty)).WithAnyArguments();

            var fakeSignalrDbDependencyFactory = A.Fake<ISignalrDbDependencyFactory>();
            var fakeSignalrDbDependencyFactoryCreateDepCall = A.CallTo(() => fakeSignalrDbDependencyFactory.CreateDbDependency(null, false, 0, false)).WithAnyArguments();

            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty, new TraceSource("ss"),
                fakeDbBehavior,
                fakeDbProviderFactory,
                fakeOracleDependencyManager,
                fakeSignalrDbDependencyFactory,
                false);

            using (var cts = new CancellationTokenSource())
            {
                Task.Run(() => dbOperation.ExecuteReaderWithUpdates(fakeProcessRecordAction),
                    cts.Token);
                Thread.Sleep(1000);
                cts.Cancel();
            }

            Thread.Sleep(1000);
            fakeOracleDependencyManagerRegistryDepCall.MustNotHaveHappened();
            fakeSignalrDbDependencyFactoryCreateDepCall.MustNotHaveHappened();
            fakeProcessRecordActionInvokeCall.MustHaveHappened();
            fakeDbReaderReadCall.MustHaveHappened(Repeated.Exactly.Times(3));
            fakeDbConnectionDisposeCall.MustHaveHappened();

            dbOperation.Dispose();

            fakeOracleDependencyManagerRemoveRegistrationCall.MustHaveHappened(Repeated.Exactly.Once);
        }

        private class FakeSignalrDependency : ISignalRDbDependency
        {
            public void FireEvent()
            {
                if (OnChanged != null)
                {
                    OnChanged(this, A.Fake<ISignalRDbNotificationEventArgs>());
                }
            }

            public void FireEvent(OracleNotificationType notificationType, OracleNotificationInfo notificationInfo)
            {
                var notificationArgsFake = A.Fake<ISignalRDbNotificationEventArgs>();
                A.CallTo(() => notificationArgsFake.NotificationType).Returns((int)notificationType);
                A.CallTo(() => notificationArgsFake.NotificationInfo).Returns((int)notificationInfo);

                if (OnChanged != null)
                {
                    OnChanged(this, notificationArgsFake);
                }
            }

            public event EventHandler<ISignalRDbNotificationEventArgs> OnChanged;
            public void RemoveRegistration(OracleConnection conn)
            {
            }
        }

        [TestMethod]
        public void DependencyOnChange()
        {
            var fakeOracleDependencyManager = A.Fake<IOracleDependencyManager>();
            var fakeOracleDependencyManagerRegistryDepCall = A.CallTo(() => fakeOracleDependencyManager.RegisterDependency(null)).WithAnyArguments();

            var fakeSignalrDbDependencyFactory = A.Fake<ISignalrDbDependencyFactory>();
            var fakeSignalrDbDependencyFactoryCreateDepCall = A.CallTo(() => fakeSignalrDbDependencyFactory.CreateDbDependency(null, false, 0, false)).WithAnyArguments();

            var fakeSignalrDependency = new FakeSignalrDependency();
            fakeSignalrDbDependencyFactoryCreateDepCall.Returns(fakeSignalrDependency);

            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty, new TraceSource("ss"),
                null,
                A.Fake<IDbProviderFactory>(),
                fakeOracleDependencyManager,
                fakeSignalrDbDependencyFactory,
                true);

            dbOperation.ExecuteReaderWithUpdates(A.Fake<Action<IDataRecord, IDbOperation>>());

            int changedCounter = 0;
            dbOperation.Changed += () => { changedCounter++; };
            int faultCounter = 0;
            dbOperation.Faulted += ex => { faultCounter++; };

            fakeSignalrDependency.FireEvent();

            Assert.AreEqual(1, changedCounter);
            Assert.AreEqual(0, faultCounter);
            fakeOracleDependencyManagerRegistryDepCall.MustHaveHappened(Repeated.Exactly.Once);
            fakeSignalrDbDependencyFactoryCreateDepCall.MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void DependencyFault()
        {
            var fakeOracleDependencyManager = A.Fake<IOracleDependencyManager>();
            var fakeOracleDependencyManagerRegistryDepCall = A.CallTo(() => fakeOracleDependencyManager.RegisterDependency(null)).WithAnyArguments();

            var fakeSignalrDbDependencyFactory = A.Fake<ISignalrDbDependencyFactory>();
            var fakeSignalrDbDependencyFactoryCreateDepCall = A.CallTo(() => fakeSignalrDbDependencyFactory.CreateDbDependency(null, false, 0, false)).WithAnyArguments();

            var fakeSignalrDependency = new FakeSignalrDependency();
            fakeSignalrDbDependencyFactoryCreateDepCall.Returns(fakeSignalrDependency);

            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty, new TraceSource("ss"),
                null,
                A.Fake<IDbProviderFactory>(),
                fakeOracleDependencyManager,
                fakeSignalrDbDependencyFactory,
                true);

            dbOperation.ExecuteReaderWithUpdates(A.Fake<Action<IDataRecord, IDbOperation>>());

            int faultCounter = 0;
            dbOperation.Faulted += ex => { if (ex != null) faultCounter++; };

            fakeSignalrDependency.FireEvent(OracleNotificationType.Change, OracleNotificationInfo.Error);

            Assert.AreEqual(1, faultCounter);
            fakeOracleDependencyManagerRegistryDepCall.MustHaveHappened(Repeated.Exactly.Once);
            fakeSignalrDbDependencyFactoryCreateDepCall.MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void RemoveRegistrationDependency()
        {
            var fakeOracleDependencyManager = A.Fake<IOracleDependencyManager>();
            var fakeOracleDependencyManagerRegistryDepCall = A.CallTo(() => fakeOracleDependencyManager.RegisterDependency(null)).WithAnyArguments();

            var fakeOracleDependencyManagerRemoveRegistrationCall =
                A.CallTo(() => fakeOracleDependencyManager.RemoveRegistration(string.Empty)).WithAnyArguments();

            var fakeSignalrDbDependencyFactory = A.Fake<ISignalrDbDependencyFactory>();
            var fakeSignalrDbDependencyFactoryCreateDepCall = A.CallTo(() => fakeSignalrDbDependencyFactory.CreateDbDependency(null, false, 0, false)).WithAnyArguments();

            var fakeSignalrDependency = new FakeSignalrDependency();
            fakeSignalrDbDependencyFactoryCreateDepCall.Returns(fakeSignalrDependency);

            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty, new TraceSource("ss"),
                null,
                A.Fake<IDbProviderFactory>(),
                fakeOracleDependencyManager,
                fakeSignalrDbDependencyFactory,
                true);

            dbOperation.ExecuteReaderWithUpdates(A.Fake<Action<IDataRecord, IDbOperation>>());

            fakeSignalrDependency.FireEvent(OracleNotificationType.Subscribe, OracleNotificationInfo.Error);

            fakeOracleDependencyManagerRegistryDepCall.MustHaveHappened(Repeated.Exactly.Once);
            fakeSignalrDbDependencyFactoryCreateDepCall.MustHaveHappened(Repeated.Exactly.Once);
            fakeOracleDependencyManagerRemoveRegistrationCall.MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void ExecuteReaderWithUpdates_Fail()
        {
            var fakeOracleDependencyManager = A.Fake<IOracleDependencyManager>();
            var fakeSignalrDbDependencyFactory = A.Fake<ISignalrDbDependencyFactory>();

            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty,
                new TraceSource("ss"),
                null,
                A.Fake<IDbProviderFactory>(),
                fakeOracleDependencyManager,
                fakeSignalrDbDependencyFactory,
                true);

            dbOperation.Queried += () => { throw new Exception(); };
            int counter = 0;
            dbOperation.Faulted += (ex) => { counter++; };

            using (var cts = new CancellationTokenSource())
            {
                Task.Run(() => dbOperation.ExecuteReaderWithUpdates(A.Fake<Action<IDataRecord, IDbOperation>>()),
                    cts.Token);
                Thread.Sleep(1000);
                dbOperation.Dispose();
                cts.Cancel();
            }
           
            Assert.IsTrue(counter > 0);
        }

        [TestMethod]
        public void UpdateDependency()
        {
            var fakeOracleDependencyManager = A.Fake<IOracleDependencyManager>();
            var fakeOracleDependencyManagerRegistryDepCall =
                A.CallTo(() => fakeOracleDependencyManager.RegisterDependency(null)).WithAnyArguments();

            var fakeSignalrDbDependencyFactory = A.Fake<ISignalrDbDependencyFactory>();
            var fakeSignalrDbDependencyFactoryCreateDepCall =
                A.CallTo(() => fakeSignalrDbDependencyFactory.CreateDbDependency(null, false, 0, false))
                    .WithAnyArguments();

            var fakeSignalrDependency = new FakeSignalrDependency();
            fakeSignalrDbDependencyFactoryCreateDepCall.Returns(fakeSignalrDependency);

            TraceSource trace = new TraceSource("Fault");
            FakeTraceListener fakeListener = new FakeTraceListener();
            trace.Listeners.Add(fakeListener);
            trace.Switch.Level = SourceLevels.All;

            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty, trace,
                null,
                A.Fake<IDbProviderFactory>(),
                fakeOracleDependencyManager,
                fakeSignalrDbDependencyFactory,
                true);

            dbOperation.ExecuteReaderWithUpdates(A.Fake<Action<IDataRecord, IDbOperation>>());
            fakeSignalrDependency.FireEvent(OracleNotificationType.Change, OracleNotificationInfo.Update);
            int counter = 0;
            dbOperation.Faulted += (ex) => { counter++; };
            Assert.AreEqual(0, counter);
            Assert.IsTrue(fakeListener.Traces.Exists(item => item.StartsWith("Oracle notification details:")));

            fakeSignalrDependency.FireEvent(OracleNotificationType.Change, OracleNotificationInfo.End);
            Assert.AreEqual(0, counter);
            Assert.IsTrue(fakeListener.Traces.Exists(item => item.StartsWith("Oracle notification timed out")));

            fakeSignalrDependency.FireEvent(OracleNotificationType.Change, OracleNotificationInfo.Drop);
            Assert.AreEqual(1, counter);
            Assert.IsTrue(fakeListener.Traces.Exists(item => item.StartsWith("Unexpected Oracle notification details:")));
            
            fakeOracleDependencyManagerRegistryDepCall.MustHaveHappened(Repeated.AtLeast.Once);
            fakeSignalrDbDependencyFactoryCreateDepCall.MustHaveHappened(Repeated.AtLeast.Once);

        }

        [TestMethod]
        public void CatchQLreceiveloop()
        {
            
            // Configure IDbBehaviour to issue OracleException during AddOracleDependency
            var fakeDbBehavior = A.Fake<IDbBehavior>();
            A.CallTo(() => fakeDbBehavior.AddOracleDependency(null, null))
                .WithAnyArguments()
                .Throws(new Exception("Hello, World!") );
            A.CallTo(() => fakeDbBehavior.UpdateLoopRetryDelays).Returns(_updateLoopRetryDelays);

            // Define trace source with our listener to collect trace messages
            var traceSource = new TraceSource("ss");
            FakeTraceListener fakeListener = new FakeTraceListener();
            traceSource.Listeners.Add(fakeListener);
            traceSource.Switch.Level = SourceLevels.All;

            var fakeOracleDependencyManager = A.Fake<IOracleDependencyManager>();
            var fakeSignalrDbDependencyFactory = A.Fake<ISignalrDbDependencyFactory>();
            var fakeDbProviderFactory = A.Fake<IDbProviderFactory>();

            ObservableDbOperation dbOperation = new ObservableDbOperation(string.Empty, string.Empty, traceSource,
                fakeDbBehavior,
                fakeDbProviderFactory,
                fakeOracleDependencyManager,
                fakeSignalrDbDependencyFactory,
                useOracleDependency: true);

            Action<object> action = (object obj) =>
            {
                dbOperation.ExecuteReaderWithUpdates(A.Fake<Action<IDataRecord, IDbOperation>>());
            };

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            Task task = new Task(action, "ss", token);
            task.Start();
            task.Wait(TimeSpan.FromMilliseconds(5000));
            source.Cancel();
            Assert.IsNull(task.Exception);
            Assert.IsNotInstanceOfType(task.Exception, typeof(AggregateException));
            Assert.IsTrue(fakeListener.Traces.Exists(item => item.StartsWith("Error in SQL receive loop")));
            
        }

        private class FakeTraceListener : TraceListener
        {
            public readonly List<string> Traces = new List<string>();

            public override void Write(string message)
            {
                Traces.Add(message);
            }

            public override void WriteLine(string message)
            {
                Traces.Add(message);
            }
        }

    }
}
