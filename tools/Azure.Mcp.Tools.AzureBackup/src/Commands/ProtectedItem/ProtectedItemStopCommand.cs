// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;

public sealed class ProtectedItemStopCommand(ILogger<ProtectedItemStopCommand> logger) : BaseProtectedItemCommand<ProtectedItemStopOptions>()
{
    private const string CommandTitle = "Stop Protection";
    private readonly ILogger<ProtectedItemStopCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678b0";
    public override string Name => "stop";
    public override string Description => "Stops protection for a backup item with option to retain or delete backup data.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Mode);
    }

    protected override ProtectedItemStopOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Mode = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Mode.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.StopProtectionAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.ProtectedItem!, options.Mode ?? "RetainData", options.VaultType, options.Container, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new ProtectedItemStopCommandResult(result), AzureBackupJsonContext.Default.ProtectedItemStopCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error stopping protection"); HandleException(context, ex); }
        return context.Response;
    }

    internal record ProtectedItemStopCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
