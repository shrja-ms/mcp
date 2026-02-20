// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Vault;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Vault;

public sealed class VaultDeleteCommand(ILogger<VaultDeleteCommand> logger) : BaseAzureBackupCommand<VaultDeleteOptions>()
{
    private const string CommandTitle = "Delete Backup Vault";
    private readonly ILogger<VaultDeleteCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678a1";
    public override string Name => "delete";
    public override string Description => "Deletes a vault after verifying all protected items are removed and soft-deleted items are purged.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Force);
    }

    protected override VaultDeleteOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ForceDelete = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Force.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var force = string.Equals(options.ForceDelete, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.DeleteVaultAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.VaultType, force, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new VaultDeleteCommandResult(result), AzureBackupJsonContext.Default.VaultDeleteCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting vault"); HandleException(context, ex); }
        return context.Response;
    }

    internal record VaultDeleteCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
