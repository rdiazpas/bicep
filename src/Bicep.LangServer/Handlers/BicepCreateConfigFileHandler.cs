// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bicep.Core.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Bicep.LanguageServer.Handlers
{
    [Method("bicep/createConfigFile", Direction.ClientToServer)]
    public class BicepCreateConfigParams : IRequest<bool>
    {
        public DocumentUri? DestinationPath { get; init; }
    }

    /// <summary>
    /// Handles a request from the client to create a bicep configuration file
    /// </summary>
    public class BicepCreateConfigFileHandler : IJsonRpcRequestHandler<BicepCreateConfigParams, bool>
    {
        private readonly ILogger<BicepCreateConfigFileHandler> logger;

        public BicepCreateConfigFileHandler(ILogger<BicepCreateConfigFileHandler> logger)
        {
            this.logger = logger;
        }

        public Task<bool> Handle(BicepCreateConfigParams request, CancellationToken cancellationToken)
        {
            throw new Exception($"request.DestinationPath?.ToUnencodedString(): {request.DestinationPath?.ToUnencodedString()}");
        }
    }
}
