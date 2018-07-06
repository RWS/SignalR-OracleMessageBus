using System.Diagnostics;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sdl.SignalR.OracleMessageBus.Tests
{
    [TestClass]
    public class OracleInstallerTest
    {
        [TestMethod]
        public void Install_Basic_Success()
        {
            var fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            var fakeDbOperationFactory = A.Fake<IDbOperationFactory>();

            var fakeDbOperation = A.Fake<IDbOperation>();
            var createDbOperationCall =
                A.CallTo(() => fakeDbOperationFactory.CreateDbOperation("connectionString", "databaseScript", null, fakeDbProviderFactory));
            createDbOperationCall.WithAnyArguments().Returns(fakeDbOperation);

            OracleInstaller installer = new OracleInstaller("USER ID=TestUser", new TraceSource("ts"), fakeDbProviderFactory, fakeDbOperationFactory);
            installer.Install();

            createDbOperationCall.MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => fakeDbOperation.ExecuteNonQuery()).MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
