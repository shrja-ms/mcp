// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.RecoveryPoint;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.RecoveryPoint;

public sealed class RecoveryPointGetCommand(ILogger<RecoveryPointGetCommand> logger) : BaseProtectedItemCommand<RecoveryPointGetOptions>()
{
    private const string CommandTitle = "Get Recovery Point";
    private readonly ILogger<RecoveryPointGetCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef1234567898";
    public override string Name => "get";
    public override string Description =>
        """
        Retrieves detailed information about a specific recovery point, including its time and type.
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
        command.Options.Add(AzureBackupOptionDefinitions.RecoveryPoint);
    }

    protected override RecoveryPointGetOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.RecoveryPoint = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RecoveryPoint.Name);
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
            var rp = await service.GetRecoveryPointAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.ProtectedItem!,
                options.RecoveryPoint!,
                options.VaultType,
                options.Container,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new RecoveryPointGetCommandResult(rp),
                AzureBackupJsonContext.Default.RecoveryPointGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recovery point. RecoveryPoint: {RecoveryPoint}, Vault: {Vault}",
                options.RecoveryPoint, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        RequestFailedException reqEx when reqEx.Status == (int)HttpStatusCode.NotFound =>
            "Recovery point not found. Verify the recovery point ID and protected item.",
        RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    internal record RecoveryPointGetCommandResult([property: JsonPropertyName("recoveryPoint")] RecoveryPointInfo RecoveryPoint);
}
