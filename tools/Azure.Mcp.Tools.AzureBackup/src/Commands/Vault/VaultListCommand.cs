// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Commands.Subscription;
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

public sealed class VaultListCommand(ILogger<VaultListCommand> logger) : SubscriptionCommand<VaultListOptions>()
{
    private const string CommandTitle = "List Backup Vaults";
    private readonly ILogger<VaultListCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef1234567890";
    public override string Name => "list";
    public override string Description =>
        """
        Lists all backup vaults (both Recovery Services vaults and Backup vaults) in the specified subscription.
        Optionally filter by vault type using --vault-type ('rsv' or 'dpp'). If omitted, both types are listed.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = false, Idempotent = true, OpenWorld = false,
        ReadOnly = true, LocalRequired = false, Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.VaultType);
    }

    protected override VaultListOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.VaultType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.VaultType.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid)
        {
            return context.Response;
        }

        var options = BindOptions(parseResult);

        try
        {
            var service = context.GetService<IAzureBackupService>();
            var vaults = await service.ListVaultsAsync(
                options.Subscription!,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new VaultListCommandResult(vaults),
                AzureBackupJsonContext.Default.VaultListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backup vaults. Subscription: {Subscription}", options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record VaultListCommandResult([property: JsonPropertyName("vaults")] List<BackupVaultInfo> Vaults);
}
