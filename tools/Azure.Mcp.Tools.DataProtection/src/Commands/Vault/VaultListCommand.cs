// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Models.Option;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Options.Vault;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.DataProtection.Commands.Vault;

public sealed class VaultListCommand(ILogger<VaultListCommand> logger) : SubscriptionCommand<VaultListOptions>()
{
    private readonly ILogger<VaultListCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f50";

    public override string Name => "list";

    public override string Description =>
        "Lists all Azure Backup vaults in a subscription. Returns vault name, location, provisioning state, and storage settings.";

    public override string Title => "List Backup Vaults";

    public override ToolMetadata Metadata => new()
    {
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        LocalRequired = false,
        Secret = false
    };

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid)
        {
            return context.Response;
        }

        var options = BindOptions(parseResult);

        try
        {
            var service = context.GetService<IDataProtectionService>();
            var vaults = await service.ListVaultsAsync(
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new VaultListCommandResult(vaults?.ToList() ?? []),
                DataProtectionJsonContext.Default.VaultListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backup vaults. Subscription: {Subscription}", options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record VaultListCommandResult(List<BackupVaultModel> Vaults);
}
