// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Models.Option;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Monitoring;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Monitoring;

public sealed class MonitoringReportsCommand(ILogger<MonitoringReportsCommand> logger) : SubscriptionCommand<MonitoringReportsOptions>()
{
    private const string CommandTitle = "Generate Backup Reports";
    private readonly ILogger<MonitoringReportsCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678c5";
    public override string Name => "reports";
    public override string Description => "Generates backup reports from Log Analytics workspace data.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.ReportType);
        command.Options.Add(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceId);
        command.Options.Add(AzureBackupOptionDefinitions.TimeRangeDays);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
    }

    protected override MonitoringReportsOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ReportType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ReportType.Name);
        options.LogAnalyticsWorkspaceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceId.Name);
        options.TimeRangeDays = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TimeRangeDays.Name);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.GetBackupReportsAsync(options.ReportType!, options.LogAnalyticsWorkspaceId!, options.TimeRangeDays, options.WorkloadType, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new MonitoringReportsCommandResult(result), AzureBackupJsonContext.Default.MonitoringReportsCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error generating reports"); HandleException(context, ex); }
        return context.Response;
    }

    internal record MonitoringReportsCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
