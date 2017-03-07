using System.Diagnostics;
using FakeItEasy;
using FakeItEasy.ExtensionSyntax.Full;
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
                fakeDbOperationFactory.CallsTo(
                    f => f.CreateDbOperation("connectionString", "databaseScript", null, fakeDbProviderFactory));
            createDbOperationCall.WithAnyArguments().Returns(fakeDbOperation);

            OracleInstaller installer = new OracleInstaller("USER ID=TestUser", new TraceSource("ts"), fakeDbProviderFactory, fakeDbOperationFactory);
            installer.Install();

            createDbOperationCall.MustHaveHappened(Repeated.Exactly.Once);
            fakeDbOperation.CallsTo(f => f.ExecuteNonQuery()).MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
