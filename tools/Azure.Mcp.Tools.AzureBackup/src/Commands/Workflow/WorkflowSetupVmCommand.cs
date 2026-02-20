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

public sealed class WorkflowSetupVmCommand(ILogger<WorkflowSetupVmCommand> logger) : SubscriptionCommand<WorkflowSetupVmOptions>()
{
    private const string CommandTitle = "Setup VM Backup End-to-End";
    private readonly ILogger<WorkflowSetupVmCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e0";
    public override string Name => "setupvm";
    public override string Description => "Creates vault, configures security, creates policy, enables backup for VMs, and triggers optional first backup.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(OptionDefinitions.Common.ResourceGroup.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.ResourceIds);
        command.Options.Add(AzureBackupOptionDefinitions.Location);
        command.Options.Add(AzureBackupOptionDefinitions.Vault);
        command.Options.Add(AzureBackupOptionDefinitions.ScheduleFrequency);
        command.Options.Add(AzureBackupOptionDefinitions.DailyRetentionDays);
        command.Options.Add(AzureBackupOptionDefinitions.TriggerFirstBackup);
        command.Options.Add(AzureBackupOptionDefinitions.OutputIac);
    }

    protected override WorkflowSetupVmOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup ??= parseResult.GetValueOrDefault<string>(OptionDefinitions.Common.ResourceGroup.Name);
        options.ResourceIds = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceIds.Name);
        options.Location = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Location.Name);
        options.Vault = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Vault.Name);
        options.ScheduleFrequency = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ScheduleFrequency.Name);
        options.DailyRetentionDays = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.DailyRetentionDays.Name);
        options.TriggerFirstBackup = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TriggerFirstBackup.Name);
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
            var triggerFirstBackup = string.Equals(options.TriggerFirstBackup, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.SetupVmBackupAsync(options.ResourceIds!, options.ResourceGroup!, options.Subscription!, options.Location!, options.Vault, options.ScheduleFrequency, options.DailyRetentionDays, triggerFirstBackup, options.OutputIac, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowSetupVmCommandResult(result), AzureBackupJsonContext.Default.WorkflowSetupVmCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in VM backup setup workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowSetupVmCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
