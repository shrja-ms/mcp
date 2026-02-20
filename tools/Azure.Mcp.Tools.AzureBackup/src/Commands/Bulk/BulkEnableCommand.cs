// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Models.Option;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Bulk;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Bulk;

public sealed class BulkEnableCommand(ILogger<BulkEnableCommand> logger) : SubscriptionCommand<BulkEnableOptions>()
{
    private const string CommandTitle = "Bulk Enable Backup";
    private readonly ILogger<BulkEnableCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678eb";
    public override string Name => "enable";
    public override string Description => "Enables backup for multiple resources matching filter criteria.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Vault);
        command.Options.Add(AzureBackupOptionDefinitions.VaultType);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
        command.Options.Add(AzureBackupOptionDefinitions.Policy);
        command.Options.Add(AzureBackupOptionDefinitions.ResourceGroupFilter);
        command.Options.Add(AzureBackupOptionDefinitions.TagFilter);
        command.Options.Add(AzureBackupOptionDefinitions.ResourceIds);
    }

    protected override BulkEnableOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Vault = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Vault.Name);
        options.VaultType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.VaultType.Name);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
        options.Policy = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Policy.Name);
        options.ResourceGroupFilter = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceGroupFilter.Name);
        options.TagFilter = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TagFilter.Name);
        options.ResourceIds = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceIds.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.BulkEnableBackupAsync(options.Vault!, options.Subscription!, options.WorkloadType!, options.Policy!, options.VaultType, options.ResourceGroupFilter, options.TagFilter, options.ResourceIds, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new BulkEnableCommandResult(result), AzureBackupJsonContext.Default.BulkEnableCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error bulk enabling backups"); HandleException(context, ex); }
        return context.Response;
    }

    internal record BulkEnableCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
