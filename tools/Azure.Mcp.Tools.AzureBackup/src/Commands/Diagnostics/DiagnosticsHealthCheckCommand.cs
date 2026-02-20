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

public sealed class DiagnosticsHealthCheckCommand(ILogger<DiagnosticsHealthCheckCommand> logger) : BaseAzureBackupCommand<DiagnosticsHealthCheckOptions>()
{
    private const string CommandTitle = "Backup Health Check";
    private readonly ILogger<DiagnosticsHealthCheckCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678e2";
    public override string Name => "healthcheck";
    public override string Description => "Performs a comprehensive health check on a backup vault.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.RpoThresholdHours);
        command.Options.Add(AzureBackupOptionDefinitions.IncludeSecurityPosture);
    }

    protected override DiagnosticsHealthCheckOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.RpoThresholdHours = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RpoThresholdHours.Name);
        options.IncludeSecurityPosture = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.IncludeSecurityPosture.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.RunBackupHealthCheckAsync(
                options.Vault!, options.ResourceGroup!, options.Subscription!,
                options.VaultType,
                int.TryParse(options.RpoThresholdHours, out var r) ? r : null,
                !string.Equals(options.IncludeSecurityPosture, "false", StringComparison.OrdinalIgnoreCase),
                options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(
                new DiagnosticsHealthCheckCommandResult(result),
                AzureBackupJsonContext.Default.DiagnosticsHealthCheckCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error running health check"); HandleException(context, ex); }
        return context.Response;
    }

    internal record DiagnosticsHealthCheckCommandResult([property: JsonPropertyName("healthCheck")] HealthCheckResult HealthCheck);
}
