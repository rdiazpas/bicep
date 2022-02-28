// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Deployments.Core.Entities;
using Azure.Deployments.Core.Helpers;
using Azure.Deployments.Core.Json;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.CompilationManager;
using Bicep.LanguageServer.Utils;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace Bicep.LanguageServer.Handlers
{
    // This handler is used to generate compiled .json file for given a bicep file path.
    // It returns build succeeded/failed message, which can be displayed approriately in IDE output window
    public class BicepBuildCommandHandler : ExecuteTypedResponseCommandHandlerBase<string, string>
    {
        private readonly ICompilationManager compilationManager;
        private readonly EmitterSettings emitterSettings;
        private readonly IFeatureProvider features;
        private readonly IFileResolver fileResolver;
        private readonly IModuleDispatcher moduleDispatcher;
        private readonly INamespaceProvider namespaceProvider;
        private readonly IConfigurationManager configurationManager;

        public BicepBuildCommandHandler(ICompilationManager compilationManager, ISerializer serializer, IFeatureProvider features, EmitterSettings emitterSettings, INamespaceProvider namespaceProvider, IFileResolver fileResolver, IModuleDispatcher moduleDispatcher, IConfigurationManager configurationManager)
            : base(LangServerConstants.Build, serializer)
        {
            this.compilationManager = compilationManager;
            this.emitterSettings = emitterSettings;
            this.features = features;
            this.namespaceProvider = namespaceProvider;
            this.fileResolver = fileResolver;
            this.moduleDispatcher = moduleDispatcher;
            this.configurationManager = configurationManager;
        }

        public override Task<string> Handle(string bicepFilePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bicepFilePath))
            {
                throw new ArgumentException("Invalid input file");
            }

            DocumentUri documentUri = DocumentUri.FromFileSystemPath(bicepFilePath);
            string buildOutput = GenerateCompiledFileAndReturnBuildOutputMessage(bicepFilePath, documentUri);

            return Task.FromResult(buildOutput);
        }

        private string GenerateCompiledFileAndReturnBuildOutputMessage(string bicepFilePath, DocumentUri documentUri)
        {
            string compiledFilePath = PathHelper.GetDefaultBuildOutputPath(bicepFilePath);
            string compiledFile = Path.GetFileName(compiledFilePath);

            // If the template exists and contains bicep generator metadata, we can go ahead and replace the file.
            // If not, we'll fail the build.
            if (File.Exists(compiledFilePath) && !TemplateContainsBicepGeneratorMetadata(File.ReadAllText(compiledFilePath)))
            {
                return "Build failed. The file \"" + compiledFile + "\" already exists and was not generated by Bicep. If overwriting the file is intended, delete it manually and retry the Build command.";
            }

            var fileUri = documentUri.ToUri();
            RootConfiguration? configuration = null;

            try
            {
                configuration = this.configurationManager.GetConfiguration(fileUri);
            }
            catch (ConfigurationException exception)
            {
                // Fail the build if there's configuration errors.
                return exception.Message;
            }

            CompilationContext? context = compilationManager.GetCompilation(fileUri);
            Compilation compilation;

            if (context is null)
            {
                SourceFileGrouping sourceFileGrouping = SourceFileGroupingBuilder.Build(this.fileResolver, this.moduleDispatcher, new Workspace(), fileUri, configuration);
                compilation = new Compilation(features, namespaceProvider, sourceFileGrouping, configuration, new LinterAnalyzer(configuration));
            }
            else
            {
                compilation = context.Compilation;
            }

            KeyValuePair<BicepFile, IEnumerable<IDiagnostic>> diagnosticsByFile = compilation.GetAllDiagnosticsByBicepFile()
                .FirstOrDefault(x => x.Key.FileUri == fileUri);

            if (diagnosticsByFile.Value.Any(x => x.Level == DiagnosticLevel.Error))
            {
                return "Build failed. Please fix below errors:\n" + DiagnosticsHelper.GetDiagnosticsMessage(diagnosticsByFile);
            }

            using var fileStream = new FileStream(compiledFilePath, FileMode.Create, FileAccess.ReadWrite);
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), emitterSettings);
            EmitResult result = emitter.Emit(fileStream);

            return "Build succeeded. Created file " + compiledFile;
        }

        // Returns true if the template contains bicep _generator metadata, false otherwise
        public bool TemplateContainsBicepGeneratorMetadata(string template)
        {
            try
            {
                if (!string.IsNullOrEmpty(template))
                {
                    JToken jtoken = template.FromJson<JToken>();
                    if (TemplateHelpers.TryGetTemplateGeneratorObject(jtoken, out DeploymentTemplateGeneratorMetadata generator))
                    {
                        if (generator.Name == "bicep")
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}
