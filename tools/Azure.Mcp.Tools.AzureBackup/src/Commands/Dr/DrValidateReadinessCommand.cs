// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Dr;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Dr;

public sealed class DrValidateReadinessCommand(ILogger<DrValidateReadinessCommand> logger) : BaseAzureBackupCommand<DrValidateReadinessOptions>()
{
    private const string CommandTitle = "Validate DR Readiness";
    private readonly ILogger<DrValidateReadinessCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678d6";
    public override string Name => "validatereadiness";
    public override string Description => "Validates disaster recovery readiness for a vault.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.ResourceIds);
    }

    protected override DrValidateReadinessOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceIds = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceIds.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.ValidateDrReadinessAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.VaultType, options.ResourceIds, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new DrValidateReadinessCommandResult(result), AzureBackupJsonContext.Default.DrValidateReadinessCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error validating DR readiness"); HandleException(context, ex); }
        return context.Response;
    }

    internal record DrValidateReadinessCommandResult([property: JsonPropertyName("validation")] DrValidationResult Validation);
}
