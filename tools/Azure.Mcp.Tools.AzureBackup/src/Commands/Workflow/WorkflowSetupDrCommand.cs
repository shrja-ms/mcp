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

public sealed class WorkflowSetupDrCommand(ILogger<WorkflowSetupDrCommand> logger) : SubscriptionCommand<WorkflowSetupDrOptions>()
{
    private const string CommandTitle = "Setup Disaster Recovery End-to-End";
    private readonly ILogger<WorkflowSetupDrCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e5";
    public override string Name => "setupdr";
    public override string Description => "Configures cross-region restore, enables GRS redundancy, and sets up disaster recovery for resources across paired regions.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(OptionDefinitions.Common.ResourceGroup.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.ResourceIds);
        command.Options.Add(AzureBackupOptionDefinitions.Location);
        command.Options.Add(AzureBackupOptionDefinitions.Vault);
        command.Options.Add(AzureBackupOptionDefinitions.SecondaryRegion);
        command.Options.Add(AzureBackupOptionDefinitions.OutputIac);
    }

    protected override WorkflowSetupDrOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup ??= parseResult.GetValueOrDefault<string>(OptionDefinitions.Common.ResourceGroup.Name);
        options.ResourceIds = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceIds.Name);
        options.Location = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Location.Name);
        options.Vault = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Vault.Name);
        options.SecondaryRegion = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.SecondaryRegion.Name);
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
            var result = await service.SetupDisasterRecoveryAsync(options.ResourceIds!, options.Location!, options.ResourceGroup!, options.Subscription!, options.Vault, options.SecondaryRegion, options.OutputIac, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowSetupDrCommandResult(result), AzureBackupJsonContext.Default.WorkflowSetupDrCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in disaster recovery setup workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowSetupDrCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
