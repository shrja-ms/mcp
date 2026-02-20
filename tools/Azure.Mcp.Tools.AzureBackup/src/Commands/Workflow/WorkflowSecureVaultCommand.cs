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

public sealed class WorkflowSecureVaultCommand(ILogger<WorkflowSecureVaultCommand> logger) : BaseAzureBackupCommand<WorkflowSecureVaultOptions>()
{
    private const string CommandTitle = "Secure Vault End-to-End";
    private readonly ILogger<WorkflowSecureVaultCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e4";
    public override string Name => "securevault";
    public override string Description => "Hardens vault security by configuring immutability, soft delete, MUA with Resource Guard, CMK encryption, and diagnostics.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.SecurityLevel);
        command.Options.Add(AzureBackupOptionDefinitions.ResourceGuardId);
        command.Options.Add(AzureBackupOptionDefinitions.KeyVaultUri);
        command.Options.Add(AzureBackupOptionDefinitions.KeyName);
        command.Options.Add(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceId);
    }

    protected override WorkflowSecureVaultOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.SecurityLevel = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.SecurityLevel.Name);
        options.ResourceGuardId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceGuardId.Name);
        options.KeyVaultUri = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.KeyVaultUri.Name);
        options.KeyName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.KeyName.Name);
        options.LogAnalyticsWorkspaceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceId.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.SecureVaultAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.VaultType, options.SecurityLevel, options.ResourceGuardId, options.KeyVaultUri, options.KeyName, options.LogAnalyticsWorkspaceId, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new WorkflowSecureVaultCommandResult(result), AzureBackupJsonContext.Default.WorkflowSecureVaultCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error in secure vault workflow"); HandleException(context, ex); }
        return context.Response;
    }

    internal record WorkflowSecureVaultCommandResult([property: JsonPropertyName("workflow")] WorkflowResult Workflow);
}
