// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.RecoveryPoint;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.RecoveryPoint;

public sealed class RecoveryPointArchiveCommand(ILogger<RecoveryPointArchiveCommand> logger) : BaseProtectedItemCommand<RecoveryPointArchiveOptions>()
{
    private const string CommandTitle = "Archive Recovery Point";
    private readonly ILogger<RecoveryPointArchiveCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678b7";
    public override string Name => "archive";
    public override string Description => "Moves or checks eligibility of a recovery point for archive tier.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.RecoveryPoint);
        command.Options.Add(AzureBackupOptionDefinitions.CheckEligibilityOnly);
    }

    protected override RecoveryPointArchiveOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.RecoveryPoint = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RecoveryPoint.Name);
        options.CheckEligibilityOnly = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.CheckEligibilityOnly.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var checkOnly = string.Equals(options.CheckEligibilityOnly, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.MoveRecoveryPointToArchiveAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.ProtectedItem!, options.RecoveryPoint, options.Container, checkOnly, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new RecoveryPointArchiveCommandResult(result), AzureBackupJsonContext.Default.RecoveryPointArchiveCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error archiving recovery point"); HandleException(context, ex); }
        return context.Response;
    }

    internal record RecoveryPointArchiveCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
