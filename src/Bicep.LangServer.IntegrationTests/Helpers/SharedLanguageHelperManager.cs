// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Bicep.LangServer.IntegrationTests.Helpers
{
    public static class SharedLanguageHelperManager
    {
        private static readonly ConcurrentDictionary<Assembly, ConcurrentDictionary<Type, LanguageServerHelper>> assemblies = new();

        private static readonly ConcurrentDictionary<Type, LanguageServerHelper> languageServers = new();

        public static void RegisterAssembly(Assembly assembly)
        {
            if(assemblies.TryAdd(assembly, new ConcurrentDictionary<Type, LanguageServerHelper>()))
            {
                return;
            }

            throw new AssertFailedException($"The assembly '{assembly.FullName}' is already registered. Call {nameof(UnregisterAssembly)}() first.");
        }

        public static void UnregisterAssembly(Assembly assembly)
        {
            if (assemblies.TryRemove(assembly, out var servers))
            {
                // only enumerate the dictionary once because there can be race conditions
                // if we have misbehaving test code
                var remainingTypes = servers.Keys.Select(type => type.Name).ToList();
                if (remainingTypes.Any())
                {
                    throw new AssertFailedException($"All types for assembly '{assembly.FullName}' have not been unregistered. The following types remain: {remainingTypes.ConcatString(", ")}");
                }
            }

            throw new AssertFailedException($"The assembly '{assembly.FullName}' was not registered. Call {nameof(RegisterAssembly)}() first.");
        }

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
