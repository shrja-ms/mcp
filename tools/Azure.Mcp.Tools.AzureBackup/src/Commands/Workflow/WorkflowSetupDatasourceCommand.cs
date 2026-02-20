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

public sealed class WorkflowSetupDatasourceCommand(ILogger<WorkflowSetupDatasourceCommand> logger) : SubscriptionCommand<WorkflowSetupDatasourceOptions>()
{
    private const string CommandTitle = "Setup Datasource Backup End-to-End";
    private readonly ILogger<WorkflowSetupDatasourceCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e3";
    public override string Name => "setupdatasource";
    public override string Description => "Creates vault, configures policy, and enables backup for a generic datasource such as Disk, Blob, PostgreSQL, or MySQL.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(OptionDefinitions.Common.ResourceGroup.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.DatasourceId);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
        command.Options.Add(AzureBackupOptionDefinitions.Location);
        command.Options.Add(AzureBackupOptionDefinitions.Vault);
        command.Options.Add(AzureBackupOptionDefinitions.OutputIac);
    }

    protected override WorkflowSetupDatasourceOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup ??= parseResult.GetValueOrDefault<string>(OptionDefinitions.Common.ResourceGroup.Name);
        options.DatasourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.DatasourceId.Name);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
        options.Location = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Location.Name);
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
            var result = await service.SetupDatasourceBackupAsync(options.DatasourceId!, options.WorkloadType!, options.ResourceGroup!, options.Subscription!, options.Location!, options.Vault, options.OutputIac, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowSetupDatasourceCommandResult(result), AzureBackupJsonContext.Default.WorkflowSetupDatasourceCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in datasource backup setup workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowSetupDatasourceCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
