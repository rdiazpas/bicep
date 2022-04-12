// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Bicep.LangServer.IntegrationTests
{
    [TestClass]
    public class TestTests
    {
        [NotNull]
        public TestContext? TestContext { get; set; }

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            await Task.Yield();
            context.WriteLine("Class init");
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            await Task.Yield();
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            await Task.Yield();
            this.TestContext.WriteLine($"test init {this.TestContext.TestName} -> {this.GetHashCode()}");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.TestContext.WriteLine($"test cleanup {this.TestContext.TestName} -> {this.GetHashCode()}");
        }

        [TestMethod]
        public void SyncTest()
        {
            this.TestContext.WriteLine($"Sync Test -> {this.GetHashCode()}");
        }

        [TestMethod]
        public async Task AsyncTest()
        {
            await Task.Yield();
            this.TestContext.WriteLine($"Async Test -> {this.GetHashCode()}");
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataTestMethod]
        public async Task AsyncDataTest(int row)
        {
            await Task.Yield();
            this.TestContext.WriteLine($"Async Data Test {row} -> {this.GetHashCode()}");
        }
    }
}
