// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;

public sealed class ProtectedItemListCommand(ILogger<ProtectedItemListCommand> logger) : BaseAzureBackupCommand<ProtectedItemListOptions>()
{
    private const string CommandTitle = "List Protected Items";
    private readonly ILogger<ProtectedItemListCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef1234567899";
    public override string Name => "list";
    public override string Description =>
        """
        Lists all protected items (backup instances) in the specified vault, including
        protection status, datasource information, and policy details.
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
            var items = await service.ListProtectedItemsAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new ProtectedItemListCommandResult(items),
                AzureBackupJsonContext.Default.ProtectedItemListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing protected items. Vault: {Vault}", options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record ProtectedItemListCommandResult([property: JsonPropertyName("protectedItems")] List<ProtectedItemInfo> ProtectedItems);
}
