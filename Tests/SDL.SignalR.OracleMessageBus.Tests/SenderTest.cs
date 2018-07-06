using System.Data;
using System.Diagnostics;
using System.Collections.Generic;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNet.SignalR.Messaging;

namespace Sdl.SignalR.OracleMessageBus.Tests
{
    [TestClass]
    public class SenderTest
    {
        [TestMethod]
        public void Send_EmptyList_Success()
        {
            var fakeDbOperation = A.Fake<IDbOperation>();
            var executeNonQueryCall = A.CallTo(() => fakeDbOperation.ExecuteNonQueryAsync());

            var fakeDbOperationFactory = A.Fake<IDbOperationFactory>();
            A.CallTo(() => fakeDbOperationFactory.CreateDbOperation("connStr", "table", new TraceSource("ts"), A.Fake<IDbProviderFactory>()))
                                  .WithAnyArguments()
                                  .Returns(fakeDbOperation);

            IOracleSender sender = new OracleSender("connStr", new TraceSource("ts"), A.Fake<IDbProviderFactory>(), fakeDbOperationFactory);
            sender.Send(new List<Message>()).Wait();

            executeNonQueryCall.MustNotHaveHappened();
        }

        [TestMethod]
        public void Send_OneMessage_Success()
        {
            var fakeDbOperation = A.Fake<IDbOperation>();
            var executeNonQueryCall = A.CallTo(() => fakeDbOperation.ExecuteNonQueryAsync());

            var fakeDbOperationFactory = A.Fake<IDbOperationFactory>();
            A.CallTo(() => fakeDbOperationFactory.CreateDbOperation("connStr", "table", new TraceSource("ts"), A.Fake<IDbProviderFactory>(), A.Fake<IDataParameter>()))
                                  .WithAnyArguments()
                                  .Returns(fakeDbOperation);

            var messages = new List<Message>();
            messages.Add(new Message("src", "key", "val"));

            IOracleSender sender = new OracleSender("connStr", new TraceSource("ts"), A.Fake<IDbProviderFactory>(), fakeDbOperationFactory);
            sender.Send(messages);

            executeNonQueryCall.MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void Send_MultipleMessages_Success()
        {
            var fakeDbOperation = A.Fake<IDbOperation>();
            var executeNonQueryCall = A.CallTo(() => fakeDbOperation.ExecuteNonQueryAsync());

            var fakeDbOperationFactory = A.Fake<IDbOperationFactory>();
            A.CallTo(() => fakeDbOperationFactory.CreateDbOperation("connStr", "table", new TraceSource("ts"), A.Fake<IDbProviderFactory>(), A.Fake<IDataParameter>()))
                                  .WithAnyArguments()
                                  .Returns(fakeDbOperation);

            var messages = new List<Message>();
            messages.Add(new Message("src1", "key1", "val1"));
            messages.Add(new Message("src2", "key2", "val2"));
            messages.Add(new Message("src3", "key3", "val3"));

            IOracleSender sender = new OracleSender("connStr", new TraceSource("ts"), A.Fake<IDbProviderFactory>(), fakeDbOperationFactory);
            sender.Send(messages);

            executeNonQueryCall.MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
