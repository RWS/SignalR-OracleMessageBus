using System.Data.Common;
using FakeItEasy;
using FakeItEasy.ExtensionSyntax.Full;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sdl.SignalR.OracleMessageBus.Tests
{
    [TestClass]
    public class DbProviderFactoryAdapterTest
    {
        [TestMethod]
        public void DbProviderFactoryAdapterCreateConnection()
        {
            DbProviderFactory dbProviderFactory = A.Fake<DbProviderFactory>(c => c.Strict());
            DbConnection iDbConnection = A.Fake<DbConnection>();

            DbProviderFactoryAdapter dbProviderFactoryAdapter = new DbProviderFactoryAdapter(dbProviderFactory);
            var fakeDbProviderFactoryAdapter = dbProviderFactory.CallsTo(c => c.CreateConnection());

            fakeDbProviderFactoryAdapter.Returns(iDbConnection);
            dbProviderFactoryAdapter.CreateConnection();

            fakeDbProviderFactoryAdapter.MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
