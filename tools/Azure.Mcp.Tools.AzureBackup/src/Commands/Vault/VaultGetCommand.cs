// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Models.Option;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Vault;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Vault;

public sealed class VaultGetCommand(ILogger<VaultGetCommand> logger) : BaseAzureBackupCommand<VaultGetOptions>()
{
    private const string CommandTitle = "Get Backup Vault";
    private readonly ILogger<VaultGetCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef1234567891";
    public override string Name => "get";
    public override string Description =>
        """
        Retrieves detailed information about a specific backup vault, including its type (RSV or DPP),
        location, provisioning state, SKU, and storage redundancy settings.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = false, Idempotent = true, OpenWorld = false,
        ReadOnly = true, LocalRequired = false, Secret = false
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
            var service = context.GetService<IAzureBackupService>();
            var vault = await service.GetVaultAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new VaultGetCommandResult(vault),
                AzureBackupJsonContext.Default.VaultGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vault. Vault: {Vault}, ResourceGroup: {ResourceGroup}", options.Vault, options.ResourceGroup);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        KeyNotFoundException => "Vault not found. Verify the vault name, resource group, and that you have access.",
        RequestFailedException reqEx when reqEx.Status == (int)HttpStatusCode.NotFound =>
            "Vault not found. Verify the vault name and resource group.",
        RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    internal record VaultGetCommandResult([property: JsonPropertyName("vault")] BackupVaultInfo Vault);
}
