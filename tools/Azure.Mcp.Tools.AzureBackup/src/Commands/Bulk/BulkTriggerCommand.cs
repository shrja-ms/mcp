// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Bulk;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Bulk;

public sealed class BulkTriggerCommand(ILogger<BulkTriggerCommand> logger) : BaseAzureBackupCommand<BulkTriggerOptions>()
{
    private const string CommandTitle = "Bulk Trigger Backup";
    private readonly ILogger<BulkTriggerCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678ec";
    public override string Name => "trigger";
    public override string Description => "Triggers on-demand backup for all protected items in a vault.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
    }

    protected override BulkTriggerOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.BulkTriggerBackupAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.VaultType, options.WorkloadType, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new BulkTriggerCommandResult(result), AzureBackupJsonContext.Default.BulkTriggerCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error bulk triggering backup"); HandleException(context, ex); }
        return context.Response;
    }

    internal record BulkTriggerCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
