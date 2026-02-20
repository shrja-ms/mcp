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

public sealed class WorkflowSetupSqlHanaCommand(ILogger<WorkflowSetupSqlHanaCommand> logger) : SubscriptionCommand<WorkflowSetupSqlHanaOptions>()
{
    private const string CommandTitle = "Setup SQL/HANA Backup End-to-End";
    private readonly ILogger<WorkflowSetupSqlHanaCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e1";
    public override string Name => "setupsqlhana";
    public override string Description => "Creates vault, registers VM, discovers databases, configures policy, and enables backup for SQL or SAP HANA workloads.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(OptionDefinitions.Common.ResourceGroup.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.VmResourceId);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
        command.Options.Add(AzureBackupOptionDefinitions.Location);
        command.Options.Add(AzureBackupOptionDefinitions.Vault);
        command.Options.Add(AzureBackupOptionDefinitions.AutoProtect);
        command.Options.Add(AzureBackupOptionDefinitions.OutputIac);
    }

    protected override WorkflowSetupSqlHanaOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup ??= parseResult.GetValueOrDefault<string>(OptionDefinitions.Common.ResourceGroup.Name);
        options.VmResourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.VmResourceId.Name);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
        options.Location = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Location.Name);
        options.Vault = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Vault.Name);
        options.AutoProtect = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.AutoProtect.Name);
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
            var autoProtect = string.Equals(options.AutoProtect, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.SetupSqlHanaBackupAsync(options.VmResourceId!, options.WorkloadType!, options.ResourceGroup!, options.Subscription!, options.Location!, options.Vault, autoProtect, options.OutputIac, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowSetupSqlHanaCommandResult(result), AzureBackupJsonContext.Default.WorkflowSetupSqlHanaCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in SQL/HANA backup setup workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowSetupSqlHanaCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
