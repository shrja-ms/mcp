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

public sealed class WorkflowComplianceCommand(ILogger<WorkflowComplianceCommand> logger) : SubscriptionCommand<WorkflowComplianceOptions>()
{
    private const string CommandTitle = "Compliance Remediation End-to-End";
    private readonly ILogger<WorkflowComplianceCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e6";
    public override string Name => "compliance";
    public override string Description => "Scans for unprotected resources, evaluates backup compliance against policies, and optionally auto-remediates by enabling backup.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(OptionDefinitions.Common.ResourceGroup.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.ResourceGroupFilter);
        command.Options.Add(AzureBackupOptionDefinitions.ResourceTypeFilter);
        command.Options.Add(AzureBackupOptionDefinitions.TagFilter);
        command.Options.Add(AzureBackupOptionDefinitions.Vault);
        command.Options.Add(AzureBackupOptionDefinitions.Policy);
        command.Options.Add(AzureBackupOptionDefinitions.AutoRemediate);
    }

    protected override WorkflowComplianceOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup ??= parseResult.GetValueOrDefault<string>(OptionDefinitions.Common.ResourceGroup.Name);
        options.ResourceGroupFilter = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceGroupFilter.Name);
        options.ResourceTypeFilter = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceTypeFilter.Name);
        options.TagFilter = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TagFilter.Name);
        options.Vault = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Vault.Name);
        options.Policy = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Policy.Name);
        options.AutoRemediate = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.AutoRemediate.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var autoRemediate = string.Equals(options.AutoRemediate, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.ComplianceRemediationAsync(options.Subscription!, options.ResourceGroup, options.ResourceTypeFilter, options.TagFilter, options.Vault, options.Policy, autoRemediate, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowComplianceCommandResult(result), AzureBackupJsonContext.Default.WorkflowComplianceCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in compliance remediation workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowComplianceCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
