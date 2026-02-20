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
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;

public sealed class ProtectedItemUndeleteCommand(ILogger<ProtectedItemUndeleteCommand> logger) : BaseProtectedItemCommand<ProtectedItemUndeleteOptions>()
{
    private const string CommandTitle = "Undelete Protected Item";
    private readonly ILogger<ProtectedItemUndeleteCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678b3";
    public override string Name => "undelete";
    public override string Description => "Rehydrates a soft-deleted backup item.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.UndeleteProtectedItemAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.ProtectedItem!, options.VaultType, options.Container, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new ProtectedItemUndeleteCommandResult(result), AzureBackupJsonContext.Default.ProtectedItemUndeleteCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error undeleting item"); HandleException(context, ex); }
        return context.Response;
    }

    internal record ProtectedItemUndeleteCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
