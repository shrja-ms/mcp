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

public sealed class WorkflowSetupAksCommand(ILogger<WorkflowSetupAksCommand> logger) : SubscriptionCommand<WorkflowSetupAksOptions>()
{
    private const string CommandTitle = "Setup AKS Backup End-to-End";
    private readonly ILogger<WorkflowSetupAksCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e2";
    public override string Name => "setupaks";
    public override string Description => "Creates backup vault, configures snapshot resource group, creates policy, and enables backup for AKS clusters.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(OptionDefinitions.Common.ResourceGroup.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.TargetClusterId);
        command.Options.Add(AzureBackupOptionDefinitions.Location);
        command.Options.Add(AzureBackupOptionDefinitions.SnapshotResourceGroup);
        command.Options.Add(AzureBackupOptionDefinitions.Vault);
        command.Options.Add(AzureBackupOptionDefinitions.OutputIac);
    }

    protected override WorkflowSetupAksOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup ??= parseResult.GetValueOrDefault<string>(OptionDefinitions.Common.ResourceGroup.Name);
        options.ClusterResourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetClusterId.Name);
        options.Location = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Location.Name);
        options.SnapshotResourceGroup = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.SnapshotResourceGroup.Name);
        options.Vault = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Vault.Name);
        options.OutputIac = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.OutputIac.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.SetupAksBackupAsync(options.ClusterResourceId!, options.ResourceGroup!, options.Subscription!, options.Location!, options.SnapshotResourceGroup!, options.Vault, options.OutputIac, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowSetupAksCommandResult(result), AzureBackupJsonContext.Default.WorkflowSetupAksCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in AKS backup setup workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowSetupAksCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
