// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;

namespace Bicep.LangServer.IntegrationTests.Helpers
{
    public static class LanguageServerLifecycle
    {
        private static readonly ConcurrentDictionary<Type, LanguageServerHelper> languageServers = new();

        public static void Register<T>(LanguageServerHelper helper) where T : class
        {
            Type type = typeof(T);
            if (languageServers.TryAdd(type, helper))
            {
                return;
            }

            // if unique test classes are used as T, then there shouldn't be any contention on addition of items into the dictionary
            throw new AssertFailedException($"A language server was already registered for type '{type.FullName}'. Call {nameof(Unregister)}() first or use unique a test class.");
        }

        public static void Unregister<T>() where T : class
        {
            Type type = typeof(T);
            if (languageServers.TryRemove(type, out var languageServer))
            {
                languageServer.Dispose();
                return;
            }

            throw new AssertFailedException($"A language server was not registered for type '{type.FullName}'. Call {nameof(Register)}() first.");
        }

        public static LanguageServerHelper Get<T>() where T : class
        {
            Type type = typeof(T);
            if (languageServers.TryGetValue(type, out var languageServer))
            {
                return languageServer;
            }

            throw new AssertFailedException($"A language server was not registered for type '{type.FullName}'. Call {nameof(Register)}() first.");
        }
    }
}
