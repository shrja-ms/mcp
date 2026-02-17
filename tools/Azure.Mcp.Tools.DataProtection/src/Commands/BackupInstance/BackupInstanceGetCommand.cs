// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Options;
using Azure.Mcp.Tools.DataProtection.Options.BackupInstance;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.DataProtection.Commands.BackupInstance;

public sealed class BackupInstanceGetCommand(ILogger<BackupInstanceGetCommand> logger) : BaseBackupInstanceCommand<BackupInstanceGetOptions>()
{
    private readonly ILogger<BackupInstanceGetCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f53";

    public override string Name => "get";

    public override string Description =>
        "Gets details of a specific backup instance in an Azure Backup vault, including data source type, protection status, and policy information.";

    public override string Title => "Get Backup Instance";

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
            var instance = await service.GetBackupInstanceAsync(
                options.BackupInstance!,
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new BackupInstanceGetCommandResult(instance),
                DataProtectionJsonContext.Default.BackupInstanceGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup instance. Instance: {Instance}, Vault: {Vault}", options.BackupInstance, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record BackupInstanceGetCommandResult(BackupInstanceModel BackupInstance);
}
