// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options.RecoveryPoint;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.RecoveryPoint;

public sealed class RecoveryPointListCommand(ILogger<RecoveryPointListCommand> logger) : BaseProtectedItemCommand<RecoveryPointListOptions>()
{
    private const string CommandTitle = "List Recovery Points";
    private readonly ILogger<RecoveryPointListCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef1234567897";
    public override string Name => "list";
    public override string Description =>
        """
        Lists all available recovery points for a specific protected item or backup instance,
        including recovery point time and type.
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
            var points = await service.ListRecoveryPointsAsync(
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
                new RecoveryPointListCommandResult(points),
                AzureBackupJsonContext.Default.RecoveryPointListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing recovery points. ProtectedItem: {ProtectedItem}, Vault: {Vault}",
                options.ProtectedItem, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record RecoveryPointListCommandResult([property: JsonPropertyName("recoveryPoints")] List<RecoveryPointInfo> RecoveryPoints);
}
