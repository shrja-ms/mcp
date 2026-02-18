// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Backup;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Backup;

public sealed class BackupTriggerCommand(ILogger<BackupTriggerCommand> logger) : BaseProtectedItemCommand<BackupTriggerOptions>()
{
    private const string CommandTitle = "Trigger Backup";
    private readonly ILogger<BackupTriggerCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef123456789c";
    public override string Name => "trigger";
    public override string Description =>
        """
        Triggers an on-demand backup for a protected item or backup instance.
        The operation is asynchronous; use 'azurebackup job get' to monitor the backup job progress.
        Optionally specify a recovery point expiry time.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = true, Idempotent = false, OpenWorld = false,
        ReadOnly = false, LocalRequired = false, Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Expiry);
    }

    protected override BackupTriggerOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Expiry = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Expiry.Name);
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
            var result = await service.TriggerBackupAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.ProtectedItem!,
                options.VaultType,
                options.Container,
                options.Expiry,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new BackupTriggerCommandResult(result),
                AzureBackupJsonContext.Default.BackupTriggerCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering backup. ProtectedItem: {ProtectedItem}, Vault: {Vault}",
                options.ProtectedItem, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        ArgumentException argEx => argEx.Message,
        RequestFailedException reqEx when reqEx.Status == (int)HttpStatusCode.NotFound =>
            "Protected item not found. Verify the item name and vault.",
        RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    internal record BackupTriggerCommandResult([property: JsonPropertyName("result")] BackupTriggerResult Result);
}
