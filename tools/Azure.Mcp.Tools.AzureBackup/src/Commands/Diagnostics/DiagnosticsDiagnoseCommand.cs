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

public sealed class DiagnosticsDiagnoseCommand(ILogger<DiagnosticsDiagnoseCommand> logger) : BaseAzureBackupCommand<DiagnosticsDiagnoseOptions>()
{
    private const string CommandTitle = "Diagnose Backup Failures";
    private readonly ILogger<DiagnosticsDiagnoseCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e0";
    public override string Name => "diagnose";
    public override string Description => "Diagnoses backup failures for a vault and identifies root causes.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Job);
        command.Options.Add(AzureBackupOptionDefinitions.DatasourceId);
    }

    protected override DiagnosticsDiagnoseOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Job = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Job.Name);
        options.DatasourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.DatasourceId.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.DiagnoseBackupFailureAsync(
                options.Vault!, options.ResourceGroup!, options.Subscription!,
                options.VaultType, options.Job, options.DatasourceId,
                options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(
                new DiagnosticsDiagnoseCommandResult(result),
                AzureBackupJsonContext.Default.DiagnosticsDiagnoseCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error diagnosing backup failures"); HandleException(context, ex); }
        return context.Response;
    }

    internal record DiagnosticsDiagnoseCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
