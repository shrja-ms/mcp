// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Options;
using Azure.Mcp.Tools.DataProtection.Options.RecoveryPoint;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.DataProtection.Commands.RecoveryPoint;

public sealed class RecoveryPointListCommand(ILogger<RecoveryPointListCommand> logger) : BaseVaultCommand<RecoveryPointListOptions>()
{
    private readonly ILogger<RecoveryPointListCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f58";

    public override string Name => "list";

    public override string Description =>
        "Lists all recovery points for a backup instance in an Azure Backup vault. Returns recovery point name, type, and timestamp.";

    public override string Title => "List Recovery Points";

    public override ToolMetadata Metadata => new()
    {
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        LocalRequired = false,
        Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(DataProtectionOptionDefinitions.BackupInstance.AsRequired());
    }

    protected override RecoveryPointListOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.BackupInstance = parseResult.GetValueOrDefault<string>(DataProtectionOptionDefinitions.BackupInstance.Name);
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
            var service = context.GetService<IDataProtectionService>();
            var recoveryPoints = await service.ListRecoveryPointsAsync(
                options.BackupInstance!,
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new RecoveryPointListCommandResult(recoveryPoints?.ToList() ?? []),
                DataProtectionJsonContext.Default.RecoveryPointListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing recovery points. Instance: {Instance}, Vault: {Vault}", options.BackupInstance, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record RecoveryPointListCommandResult(List<RecoveryPointModel> RecoveryPoints);
}
