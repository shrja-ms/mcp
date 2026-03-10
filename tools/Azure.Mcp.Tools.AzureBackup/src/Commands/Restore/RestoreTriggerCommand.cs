// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Restore;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Restore;

public sealed class RestoreTriggerCommand(ILogger<RestoreTriggerCommand> logger) : BaseProtectedItemCommand<RestoreTriggerOptions>()
{
    private const string CommandTitle = "Trigger Restore";
    private readonly ILogger<RestoreTriggerCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef123456789d";
    public override string Name => "trigger";
    public override string Description =>
        """
        Triggers a restore operation for a protected item or backup instance.
        Use --recovery-point for discrete recovery point restore (Disk, PGFlex, AKS, IaaS VM).
        Use --point-in-time for continuous/PIT restore (Blob vaulted backup).
        The operation is asynchronous; use 'azurebackup job get' to monitor the restore job progress.
        Optionally specify a target resource ID for alternate-location restore.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = true, Idempotent = false, OpenWorld = false,
        ReadOnly = false, LocalRequired = false, Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.RecoveryPoint.AsOptional());
        command.Options.Add(AzureBackupOptionDefinitions.PointInTime);
        command.Options.Add(AzureBackupOptionDefinitions.TargetResourceId);
        command.Options.Add(AzureBackupOptionDefinitions.RestoreLocation);
        command.Options.Add(AzureBackupOptionDefinitions.StagingStorageAccountId);
        command.Options.Add(AzureBackupOptionDefinitions.RestoreMode);
        command.Options.Add(AzureBackupOptionDefinitions.TargetVmName);
        command.Options.Add(AzureBackupOptionDefinitions.TargetVnetId);
        command.Options.Add(AzureBackupOptionDefinitions.TargetSubnetId);
        command.Options.Add(AzureBackupOptionDefinitions.TargetDatabaseName);
        command.Options.Add(AzureBackupOptionDefinitions.TargetInstanceName);
    }

    protected override RestoreTriggerOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.RecoveryPoint = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RecoveryPoint.Name);
        options.PointInTime = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.PointInTime.Name);
        options.TargetResourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetResourceId.Name);
        options.RestoreLocation = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RestoreLocation.Name);
        options.StagingStorageAccountId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.StagingStorageAccountId.Name);
        options.RestoreMode = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RestoreMode.Name);
        options.TargetVmName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetVmName.Name);
        options.TargetVnetId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetVnetId.Name);
        options.TargetSubnetId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetSubnetId.Name);
        options.TargetDatabaseName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetDatabaseName.Name);
        options.TargetInstanceName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetInstanceName.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid)
        {
            return context.Response;
        }

        var options = BindOptions(parseResult);

        try
        {
            if (string.IsNullOrEmpty(options.RecoveryPoint) && string.IsNullOrEmpty(options.PointInTime))
            {
                throw new ArgumentException("Either --recovery-point or --point-in-time must be specified.");
            }

            var service = context.GetService<IAzureBackupService>();
            var result = await service.TriggerRestoreAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.ProtectedItem!,
                options.RecoveryPoint,
                options.VaultType,
                options.Container,
                options.TargetResourceId,
                options.RestoreLocation,
                options.StagingStorageAccountId,
                options.PointInTime,
                options.RestoreMode,
                options.TargetVmName,
                options.TargetVnetId,
                options.TargetSubnetId,
                options.TargetDatabaseName,
                options.TargetInstanceName,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new RestoreTriggerCommandResult(result),
                AzureBackupJsonContext.Default.RestoreTriggerCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering restore. ProtectedItem: {ProtectedItem}, RecoveryPoint: {RecoveryPoint}, Vault: {Vault}",
                options.ProtectedItem, options.RecoveryPoint, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        ArgumentException argEx => argEx.Message,
        RequestFailedException reqEx when reqEx.Status == (int)HttpStatusCode.NotFound =>
            "Protected item or recovery point not found. Verify the names and vault.",
        RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    internal record RestoreTriggerCommandResult([property: JsonPropertyName("result")] RestoreTriggerResult Result);
}
