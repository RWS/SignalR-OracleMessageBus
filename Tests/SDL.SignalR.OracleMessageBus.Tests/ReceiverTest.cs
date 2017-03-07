using System.Data;
using System.Diagnostics;
using System.Threading;
using FakeItEasy;
using FakeItEasy.ExtensionSyntax.Full;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sdl.SignalR.OracleMessageBus.Tests
{
    [TestClass]
    public class ReceiverTest
    {
        [TestMethod]
        public void Receive_StartReceiving_Success()
        {
            var fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            var createParameterCall = fakeDbProviderFactory.CallsTo(f => f.CreateParameter());

            var fakeDataParameter = A.Fake<IDataParameter>();
            var fakeDataParameterValueCall = fakeDataParameter.CallsTo(f => f.Value);
            fakeDataParameterValueCall.Returns((long?) 1);
            createParameterCall.Returns(fakeDataParameter);

            var fakeDbOperation = A.Fake<IDbOperation>();
            fakeDbOperation.CallsTo(f => f.ExecuteScalar())
                           .Returns((long?)1);

            var fakeDbOperationFactory = A.Fake<IDbOperationFactory>();
            fakeDbOperationFactory.CallsTo(f => f.CreateDbOperation("connStr", "command", new TraceSource("ts"), fakeDbProviderFactory))
                                  .WithAnyArguments()
                                  .Returns(fakeDbOperation);

            IOracleReceiver receiver = new OracleReceiver("connStr", true, new TraceSource("ts"), fakeDbProviderFactory, fakeDbOperationFactory, A.Fake<IObservableDbOperationFactory>());
            receiver.GetLastPayloadId();
            receiver.StartReceivingUpdatesFromDb();
            Thread.Sleep(100);

            createParameterCall.MustHaveHappened(Repeated.AtLeast.Once);
        }

        [TestMethod]
        public void Receiver_Dispose_Success()
        {
            IObservableDbOperationFactory FakeiDbOperationFactory = A.Fake<IObservableDbOperationFactory>();
            var fakeIdbProviderFactoryOperation = A.Fake<IDbProviderFactory>();

            var fakeIDataParameter = A.Fake<IDataParameter>();
            fakeIDataParameter.CallsTo(c => c.Value).Returns((long)2);
            var fakeCallsTo = fakeIdbProviderFactoryOperation.CallsTo(c => c.CreateParameter()).Returns(fakeIDataParameter);
            IOracleReceiver oracleReceiver = new OracleReceiver(string.Empty, true, new TraceSource("ss"), fakeIdbProviderFactoryOperation, A.Fake<IDbOperationFactory>(), FakeiDbOperationFactory );
            IObservableDbOperation fakDbOperation = A.Fake<IObservableDbOperation>();
            var fake =
                FakeiDbOperationFactory.CallsTo(
                    c =>
                        c.ObservableDbOperation(string.Empty, string.Empty, true, new TraceSource("ss"),
                            A.Fake<IDbProviderFactory>())).WithAnyArguments().Returns(fakDbOperation);
           
            var fakeIObservableDbOperationDispose = fakDbOperation.CallsTo(c => c.Dispose());
            oracleReceiver.GetLastPayloadId();
            oracleReceiver.StartReceivingUpdatesFromDb();
            Thread.Sleep(100);
            oracleReceiver.Dispose();

            fakeIObservableDbOperationDispose.MustHaveHappened((Repeated.Exactly.Once));
        }
    }
}
