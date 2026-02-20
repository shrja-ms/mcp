// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Dr;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Dr;

public sealed class DrCrossRegionRestoreCommand(ILogger<DrCrossRegionRestoreCommand> logger) : BaseProtectedItemCommand<DrCrossRegionRestoreOptions>()
{
    private const string CommandTitle = "Cross-Region Restore";
    private readonly ILogger<DrCrossRegionRestoreCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678d5";
    public override string Name => "crossregionrestore";
    public override string Description => "Triggers a restore from a secondary region using cross-region restore.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.RecoveryPoint);
        command.Options.Add(AzureBackupOptionDefinitions.RestoreMode);
        command.Options.Add(AzureBackupOptionDefinitions.TargetResourceId);
        command.Options.Add(AzureBackupOptionDefinitions.SecondaryRegion);
    }

    protected override DrCrossRegionRestoreOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.RecoveryPointId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RecoveryPoint.Name);
        options.RestoreMode = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RestoreMode.Name);
        options.TargetResourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetResourceId.Name);
        options.SecondaryRegion = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.SecondaryRegion.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.TriggerCrossRegionRestoreAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.ProtectedItem!, options.RecoveryPointId!, options.RestoreMode ?? "OriginalLocation", options.TargetResourceId, options.SecondaryRegion, options.VaultType, options.Container, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new DrCrossRegionRestoreCommandResult(result), AzureBackupJsonContext.Default.DrCrossRegionRestoreCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error triggering cross-region restore"); HandleException(context, ex); }
        return context.Response;
    }

    internal record DrCrossRegionRestoreCommandResult([property: JsonPropertyName("result")] RestoreTriggerResult Result);
}
