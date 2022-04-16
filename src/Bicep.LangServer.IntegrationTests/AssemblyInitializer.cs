// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Emit;
using Bicep.LangServer.IntegrationTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Bicep.LangServer.IntegrationTests
{
    [TestClass]
    public static class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            SharedLanguageHelperManager.RegisterAssembly(Assembly.GetExecutingAssembly());
            BicepDeploymentsInterop.Initialize();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            SharedLanguageHelperManager.UnregisterAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
