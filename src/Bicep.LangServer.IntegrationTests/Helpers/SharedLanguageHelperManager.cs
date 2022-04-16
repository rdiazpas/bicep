// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bicep.LangServer.IntegrationTests.Helpers
{
    public static class SharedLanguageHelperManager<T> where T : notnull
    {
        private static readonly ConcurrentDictionary<T, AsyncLazy<MultiFileLanguageServerHelper>> languageServers = new();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD012:Provide JoinableTaskFactory where allowed", Justification = "<Pending>")]
        public static void Register(T key, Func<Task<MultiFileLanguageServerHelper>> helperCreator)
        {
            if (languageServers.TryAdd(key, new AsyncLazy<MultiFileLanguageServerHelper>(helperCreator)))
            {
                return;
            }

            // if unique test classes are used as T, then there shouldn't be any contention on addition of items into the dictionary
            throw new AssertFailedException($"A language server was already registered for key '{key}'. Call {nameof(Unregister)}() first or use unique a test class.");
        }

        public static async Task Unregister(T key)
        {
            if (languageServers.TryRemove(key, out var lazy))
            {
                (await lazy.GetValueAsync()).Dispose();
                return;
            }

            throw new AssertFailedException($"A language server was not registered for key '{key}'. Call {nameof(Register)}() first.");
        }

        public static async Task<MultiFileLanguageServerHelper> Get(T key)
        {
            Type type = typeof(T);
            if (languageServers.TryGetValue(key, out var lazy))
            {
                return (await lazy.GetValueAsync());
            }

            throw new AssertFailedException($"A language server was not registered for key '{key}'. Call {nameof(Register)}() first.");
        }
    }
}
