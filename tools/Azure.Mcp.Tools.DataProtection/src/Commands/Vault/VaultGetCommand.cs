// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Options.Vault;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.DataProtection.Commands.Vault;

public sealed class VaultGetCommand(ILogger<VaultGetCommand> logger) : BaseVaultCommand<VaultGetOptions>()
{
    private readonly ILogger<VaultGetCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f51";

    public override string Name => "get";

    public override string Description =>
        "Gets details of a specific Azure Backup vault, including name, location, provisioning state, storage type, and security settings.";

    public override string Title => "Get Backup Vault";

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
            var vault = await service.GetVaultAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new VaultGetCommandResult(vault),
                DataProtectionJsonContext.Default.VaultGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup vault. Vault: {Vault}, ResourceGroup: {ResourceGroup}", options.Vault, options.ResourceGroup);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record VaultGetCommandResult(BackupVaultModel Vault);
}
