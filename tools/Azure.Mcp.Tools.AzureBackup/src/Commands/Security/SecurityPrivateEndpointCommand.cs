// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Security;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Security;

public sealed class SecurityPrivateEndpointCommand(ILogger<SecurityPrivateEndpointCommand> logger) : BaseAzureBackupCommand<SecurityPrivateEndpointOptions>()
{
    private const string CommandTitle = "Configure Private Endpoint";
    private readonly ILogger<SecurityPrivateEndpointCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678c2";
    public override string Name => "privateendpoint";
    public override string Description => "Configures private endpoint connectivity for a backup vault.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.VnetId);
        command.Options.Add(AzureBackupOptionDefinitions.SubnetId);
    }

    protected override SecurityPrivateEndpointOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.VnetId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.VnetId.Name);
        options.SubnetId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.SubnetId.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.ConfigurePrivateEndpointAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.VnetId!, options.SubnetId!, options.VaultType, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new SecurityPrivateEndpointCommandResult(result), AzureBackupJsonContext.Default.SecurityPrivateEndpointCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error configuring private endpoint"); HandleException(context, ex); }
        return context.Response;
    }

    internal record SecurityPrivateEndpointCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
