using System;
using System.Data;
using System.Diagnostics;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sdl.SignalR.OracleMessageBus.Tests
{
    [TestClass]
    public class DbOperationTest
    {
        [TestMethod]
        public void OpenDispose_Basic_Success()
        {
            bool[] dbReaderReads = { true, true, false };

            var fakeDbReader = A.Fake<IDataReader>();
            var fakeDbReaderReadCall = A.CallTo(() => fakeDbReader.Read());
            fakeDbReaderReadCall.ReturnsNextFromSequence(dbReaderReads);

            var fakeDbCommand = A.Fake<IDbCommand>();
            A.CallTo(() => fakeDbCommand.ExecuteNonQuery()).Returns(2);
            A.CallTo(() => fakeDbCommand.ExecuteScalar()).Returns(3);
            A.CallTo(() => fakeDbCommand.ExecuteReader()).Returns(fakeDbReader);

            var fakeConnection = A.Fake<IDbConnection>();
            var fakeConnectionDisposeCall = A.CallTo(() => fakeConnection.Dispose());
            var fakeConnectionOpenCall = A.CallTo(() => fakeConnection.Open());
            A.CallTo(() => fakeConnection.CreateCommand())
                .Returns(fakeDbCommand);

            IDbProviderFactory fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            var createConnectionCall = A.CallTo(() => fakeDbProviderFactory.CreateConnection());
            createConnectionCall.Returns(fakeConnection);

            DbOperation dbOperation = new DbOperation(string.Empty, string.Empty, new TraceSource("ss"), fakeDbProviderFactory);
            dbOperation.ExecuteNonQuery();
            int readCount = dbOperation.ExecuteReader(A.Fake<Action<IDataRecord, IDbOperation>>());

            Assert.AreEqual(dbReaderReads.Length - 1, readCount);

            createConnectionCall.MustHaveHappened(Repeated.Exactly.Twice);
            fakeConnectionOpenCall.MustHaveHappened(Repeated.Exactly.Twice);
            fakeConnectionDisposeCall.MustHaveHappened(Repeated.Exactly.Twice);
        }

        [TestMethod]
        public void DbExceptionThrown()
        {
            string msg = Guid.NewGuid().ToString("N");

            var fakeDbCommand = A.Fake<IDbCommand>();
            A.CallTo(() => fakeDbCommand.ExecuteNonQuery()).Throws(new Exception(msg));
            A.CallTo(() => fakeDbCommand.ExecuteScalar()).Throws(new Exception(msg));
            A.CallTo(() => fakeDbCommand.ExecuteReader()).Throws(new Exception(msg));

            var fakeConnection = A.Fake<IDbConnection>();
            A.CallTo(() => fakeConnection.CreateCommand())
                .Returns(fakeDbCommand);

            IDbProviderFactory fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            var createConnectionCall = A.CallTo(() => fakeDbProviderFactory.CreateConnection());
            createConnectionCall.Returns(fakeConnection);

            DbOperation dbOperation = new DbOperation(string.Empty, string.Empty, new TraceSource("ss"), fakeDbProviderFactory);

            try
            {
                dbOperation.ExecuteNonQuery();
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (Exception e)
            {
                Assert.AreEqual(msg, e.Message);
            }

            try
            {
                dbOperation.ExecuteScalar();
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (Exception e)
            {
                Assert.AreEqual(msg, e.Message);
            }

            try
            {
                dbOperation.ExecuteReader(A.Fake<Action<IDataRecord, IDbOperation>>());
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (Exception e)
            {
                Assert.AreEqual(msg, e.Message);
            }

            try
            {
                dbOperation.ExecuteNonQueryAsync().Wait();
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (Exception e)
            {
                Assert.AreEqual(msg, e.Message);
            }
        }
    }
}
