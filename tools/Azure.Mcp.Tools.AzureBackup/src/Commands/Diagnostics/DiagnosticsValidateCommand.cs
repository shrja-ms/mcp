// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Diagnostics;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Diagnostics;

public sealed class DiagnosticsValidateCommand(ILogger<DiagnosticsValidateCommand> logger) : BaseAzureBackupCommand<DiagnosticsValidateOptions>()
{
    private const string CommandTitle = "Validate Backup Prerequisites";
    private readonly ILogger<DiagnosticsValidateCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e1";
    public override string Name => "validate";
    public override string Description => "Validates backup prerequisites and eligibility for a datasource.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.DatasourceId.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType.AsRequired());
        command.Options.Add(AzureBackupOptionDefinitions.Policy);
    }

    protected override DiagnosticsValidateOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.DatasourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.DatasourceId.Name);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
        options.Policy = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Policy.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.ValidateBackupPrerequisitesAsync(
                options.DatasourceId!, options.Vault!, options.ResourceGroup!, options.Subscription!,
                options.WorkloadType!, options.VaultType, options.Policy,
                options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(
                new DiagnosticsValidateCommandResult(result),
                AzureBackupJsonContext.Default.DiagnosticsValidateCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error validating backup prerequisites"); HandleException(context, ex); }
        return context.Response;
    }

    internal record DiagnosticsValidateCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
