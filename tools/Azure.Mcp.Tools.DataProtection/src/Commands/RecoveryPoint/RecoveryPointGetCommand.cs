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

public sealed class RecoveryPointGetCommand(ILogger<RecoveryPointGetCommand> logger) : BaseBackupInstanceCommand<RecoveryPointGetOptions>()
{
    private readonly ILogger<RecoveryPointGetCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f59";

    public override string Name => "get";

    public override string Description =>
        "Gets details of a specific recovery point for a backup instance in an Azure Backup vault, including type and timestamp.";

    public override string Title => "Get Recovery Point";

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
        command.Options.Add(DataProtectionOptionDefinitions.RecoveryPoint.AsRequired());
    }

    protected override RecoveryPointGetOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.RecoveryPoint = parseResult.GetValueOrDefault<string>(DataProtectionOptionDefinitions.RecoveryPoint.Name);
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
            var recoveryPoint = await service.GetRecoveryPointAsync(
                options.RecoveryPoint!,
                options.BackupInstance!,
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new RecoveryPointGetCommandResult(recoveryPoint),
                DataProtectionJsonContext.Default.RecoveryPointGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recovery point. RecoveryPoint: {RecoveryPoint}, Instance: {Instance}", options.RecoveryPoint, options.BackupInstance);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record RecoveryPointGetCommandResult(RecoveryPointModel RecoveryPoint);
}
