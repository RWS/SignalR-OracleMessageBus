using System.Data;
using System.Diagnostics;
using System.Threading;
using FakeItEasy;
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
            var createParameterCall = A.CallTo(() => fakeDbProviderFactory.CreateParameter());

            var fakeDataParameter = A.Fake<IDataParameter>();
            var fakeDataParameterValueCall = A.CallTo(() => fakeDataParameter.Value);
            fakeDataParameterValueCall.Returns((long?) 1);
            createParameterCall.Returns(fakeDataParameter);

            var fakeDbOperation = A.Fake<IDbOperation>();
            A.CallTo(() => fakeDbOperation.ExecuteScalar())
                           .Returns((long?)1);

            var fakeDbOperationFactory = A.Fake<IDbOperationFactory>();
            A.CallTo(() => fakeDbOperationFactory.CreateDbOperation("connStr", "command", new TraceSource("ts"), fakeDbProviderFactory))
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
            A.CallTo(() => fakeIDataParameter.Value).Returns((long)2);
            var fakeCallsTo = A.CallTo(() => fakeIdbProviderFactoryOperation.CreateParameter()).Returns(fakeIDataParameter);
            IOracleReceiver oracleReceiver = new OracleReceiver(string.Empty, true, new TraceSource("ss"), fakeIdbProviderFactoryOperation, A.Fake<IDbOperationFactory>(), FakeiDbOperationFactory );
            IObservableDbOperation fakDbOperation = A.Fake<IObservableDbOperation>();
            var fake =
                A.CallTo(() => FakeiDbOperationFactory.ObservableDbOperation(string.Empty, string.Empty, true, new TraceSource("ss"),
                            A.Fake<IDbProviderFactory>())).WithAnyArguments().Returns(fakDbOperation);
           
            var fakeIObservableDbOperationDispose = A.CallTo(() => fakDbOperation.Dispose());
            oracleReceiver.GetLastPayloadId();
            oracleReceiver.StartReceivingUpdatesFromDb();
            Thread.Sleep(100);
            oracleReceiver.Dispose();

            fakeIObservableDbOperationDispose.MustHaveHappened((Repeated.Exactly.Once));
        }
    }
}
