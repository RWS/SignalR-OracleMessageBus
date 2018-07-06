using System;
using System.Data;
using System.Diagnostics;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sdl.SignalR.OracleMessageBus.Tests
{
    [TestClass]
    public class OracleDependencyManagerTest
    {
        [TestMethod]
        public void Basic_Success()
        {
            OracleDependencyManager depManager = new OracleDependencyManager(A.Fake<IDbProviderFactory>(), new TraceSource("ss"));
            depManager.RegisterDependency(null);
        }

        [TestMethod]
        public void AllDependenciesAreUnregisterd()
        {
            var fakeDbDependency = A.Fake<ISignalRDbDependency>();
            var fakeDbDependencyRemoveRegistrationCall = A.CallTo(() => fakeDbDependency.RemoveRegistration(null)).WithAnyArguments();

            var fakeDbConnection = A.Fake<IDbConnection>();
            var fakeDbConnectionOpenCall = A.CallTo(() => fakeDbConnection.Open());
            var fakeDbConnectionDisposeCall = A.CallTo(() => fakeDbConnection.Dispose());

            var fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            A.CallTo(() => fakeDbProviderFactory.CreateConnection()).Returns(fakeDbConnection);
            var depManager = new OracleDependencyManager(fakeDbProviderFactory, new TraceSource("ss"));
            depManager.RegisterDependency(fakeDbDependency);
            depManager.RegisterDependency(fakeDbDependency);
            depManager.RemoveRegistration(string.Empty);

            fakeDbConnectionOpenCall.MustHaveHappened(Repeated.Exactly.Once);
            fakeDbDependencyRemoveRegistrationCall.MustHaveHappened(Repeated.Exactly.Twice);
            fakeDbConnectionDisposeCall.MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void ErrorDuringRemovingDependency()
        {
            var fakeDbDependency = A.Fake<ISignalRDbDependency>();
            var fakeDbDependencyRemoveRegistrationCall = A.CallTo(() => fakeDbDependency.RemoveRegistration(null)).WithAnyArguments();

            var fakeErrorDbDependency = A.Fake<ISignalRDbDependency>();
            A.CallTo(() => fakeDbDependency.RemoveRegistration(null)).WithAnyArguments().Throws(new Exception());

            var fakeDbConnection = A.Fake<IDbConnection>();
            var fakeDbConnectionOpenCall = A.CallTo(() => fakeDbConnection.Open());

            var fakeDbProviderFactory = A.Fake<IDbProviderFactory>();
            A.CallTo(() => fakeDbProviderFactory.CreateConnection()).Returns(fakeDbConnection);
            var depManager = new OracleDependencyManager(fakeDbProviderFactory, new TraceSource("ss"));
            depManager.RegisterDependency(fakeErrorDbDependency);
            depManager.RegisterDependency(fakeDbDependency);
            depManager.RemoveRegistration(string.Empty);

            fakeDbConnectionOpenCall.MustHaveHappened(Repeated.Exactly.Once);
            fakeDbDependencyRemoveRegistrationCall.MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}
