// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Models.Option;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Workflow;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Workflow;

public sealed class WorkflowMigrateCommand(ILogger<WorkflowMigrateCommand> logger) : SubscriptionCommand<WorkflowMigrateOptions>()
{
    private const string CommandTitle = "Migrate Backup Configuration End-to-End";
    private readonly ILogger<WorkflowMigrateCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e7";
    public override string Name => "migrate";
    public override string Description => "Migrates backup configurations from a source vault to a target vault, re-protecting items and optionally decommissioning the source.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(OptionDefinitions.Common.ResourceGroup.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.SourceVaultName);
        command.Options.Add(AzureBackupOptionDefinitions.VaultType);
        command.Options.Add(AzureBackupOptionDefinitions.ResourceGroupFilter);
        command.Options.Add(AzureBackupOptionDefinitions.TargetResourceId);
        command.Options.Add(AzureBackupOptionDefinitions.TargetVaultName);
        command.Options.Add(AzureBackupOptionDefinitions.RestoreLocation);
        command.Options.Add(AzureBackupOptionDefinitions.Force);
    }

    protected override WorkflowMigrateOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup ??= parseResult.GetValueOrDefault<string>(OptionDefinitions.Common.ResourceGroup.Name);
        options.SourceVaultName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.SourceVaultName.Name);
        options.VaultType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.VaultType.Name);
        options.ResourceGroupFilter = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceGroupFilter.Name);
        options.TargetResourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetResourceId.Name);
        options.TargetVaultName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetVaultName.Name);
        options.RestoreLocation = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RestoreLocation.Name);
        options.Force = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Force.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var force = string.Equals(options.Force, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.MigrateBackupConfigAsync(options.SourceVaultName!, options.VaultType ?? "rsv", options.ResourceGroup!, options.Subscription!, options.ResourceGroup!, options.TargetVaultName, options.RestoreLocation, force, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowMigrateCommandResult(result), AzureBackupJsonContext.Default.WorkflowMigrateCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in backup migration workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowMigrateCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
