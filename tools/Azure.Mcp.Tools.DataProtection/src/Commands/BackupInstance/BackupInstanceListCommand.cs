// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Options.BackupInstance;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.DataProtection.Commands.BackupInstance;

public sealed class BackupInstanceListCommand(ILogger<BackupInstanceListCommand> logger) : BaseVaultCommand<BackupInstanceListOptions>()
{
    private readonly ILogger<BackupInstanceListCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f52";

    public override string Name => "list";

    public override string Description =>
        "Lists all backup instances in an Azure Backup vault. Returns instance name, data source type, protection status, and policy information.";

    public override string Title => "List Backup Instances";

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
            var instances = await service.ListBackupInstancesAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new BackupInstanceListCommandResult(instances?.ToList() ?? []),
                DataProtectionJsonContext.Default.BackupInstanceListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backup instances. Vault: {Vault}", options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record BackupInstanceListCommandResult(List<BackupInstanceModel> BackupInstances);
}
