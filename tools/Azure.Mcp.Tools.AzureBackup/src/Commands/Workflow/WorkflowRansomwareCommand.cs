// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Workflow;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Workflow;

public sealed class WorkflowRansomwareCommand(ILogger<WorkflowRansomwareCommand> logger) : BaseAzureBackupCommand<WorkflowRansomwareOptions>()
{
    private const string CommandTitle = "Ransomware Recovery End-to-End";
    private readonly ILogger<WorkflowRansomwareCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e8";
    public override string Name => "ransomware";
    public override string Description => "Identifies clean recovery points before infection timestamp, validates integrity, and restores resources to an isolated environment.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.ResourceIds);
        command.Options.Add(AzureBackupOptionDefinitions.InfectionTimestamp);
        command.Options.Add(AzureBackupOptionDefinitions.Force);
    }

    protected override WorkflowRansomwareOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceIds = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceIds.Name);
        options.InfectionTimestamp = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.InfectionTimestamp.Name);
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
            var restoreToIsolatedEnv = string.Equals(options.Force, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.RansomwareRecoveryAsync(options.ResourceIds!, options.Vault!, options.ResourceGroup!, options.Subscription!, options.InfectionTimestamp!, options.VaultType, restoreToIsolatedEnv, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowRansomwareCommandResult(result), AzureBackupJsonContext.Default.WorkflowRansomwareCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in ransomware recovery workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowRansomwareCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
