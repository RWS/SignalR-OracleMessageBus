using System;
using System.Data;
using System.Diagnostics;
using FakeItEasy;
using FakeItEasy.ExtensionSyntax.Full;
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
            var fakeDbReaderReadCall = fakeDbReader.CallsTo(c => c.Read());
            fakeDbReaderReadCall.ReturnsNextFromSequence(dbReaderReads);

            var fakeDbCommand = A.Fake<IDbCommand>();
            fakeDbCommand.CallsTo(c => c.ExecuteNonQuery()).Returns(2);
            fakeDbCommand.CallsTo(c => c.ExecuteScalar()).Returns(3);
            fakeDbCommand.CallsTo(c => c.ExecuteReader()).Returns(fakeDbReader);

            var fakeConnection = A.Fake<IDbConnection>();
            var fakeConnectionDisposeCall = fakeConnection.CallsTo(c => c.Dispose());
            var fakeConnectionOpenCall = fakeConnection.CallsTo(c => c.Open());
            fakeConnection
                .CallsTo(c => c.CreateCommand())
                .Returns(fakeDbCommand);

            IDbProviderFactory fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            var createConnectionCall = fakeDbProviderFactory.CallsTo(c => c.CreateConnection());
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
            fakeDbCommand.CallsTo(c => c.ExecuteNonQuery()).Throws(new Exception(msg));
            fakeDbCommand.CallsTo(c => c.ExecuteScalar()).Throws(new Exception(msg));
            fakeDbCommand.CallsTo(c => c.ExecuteReader()).Throws(new Exception(msg));

            var fakeConnection = A.Fake<IDbConnection>();
            fakeConnection.CallsTo(c => c.CreateCommand())
                .Returns(fakeDbCommand);

            IDbProviderFactory fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            var createConnectionCall = fakeDbProviderFactory.CallsTo(c => c.CreateConnection());
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
