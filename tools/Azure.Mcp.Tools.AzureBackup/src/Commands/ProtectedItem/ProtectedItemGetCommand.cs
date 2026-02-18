// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;

public sealed class ProtectedItemGetCommand(ILogger<ProtectedItemGetCommand> logger) : BaseProtectedItemCommand<ProtectedItemGetOptions>()
{
    private const string CommandTitle = "Get Protected Item";
    private readonly ILogger<ProtectedItemGetCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef123456789a";
    public override string Name => "get";
    public override string Description =>
        """
        Retrieves detailed information about a specific protected item or backup instance,
        including protection status, datasource details, policy assignment, and last backup time.
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
            var item = await service.GetProtectedItemAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.ProtectedItem!,
                options.VaultType,
                options.Container,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new ProtectedItemGetCommandResult(item),
                AzureBackupJsonContext.Default.ProtectedItemGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting protected item. ProtectedItem: {ProtectedItem}, Vault: {Vault}",
                options.ProtectedItem, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        KeyNotFoundException => "Protected item not found. Verify the item name and vault.",
        RequestFailedException reqEx when reqEx.Status == (int)HttpStatusCode.NotFound =>
            "Protected item not found. Verify the item name and vault.",
        RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    internal record ProtectedItemGetCommandResult([property: JsonPropertyName("protectedItem")] ProtectedItemInfo ProtectedItem);
}
