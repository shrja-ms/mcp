// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Monitoring;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Monitoring;

public sealed class MonitoringConfigureCommand(ILogger<MonitoringConfigureCommand> logger) : BaseAzureBackupCommand<MonitoringConfigureOptions>()
{
    private const string CommandTitle = "Configure Monitoring";
    private readonly ILogger<MonitoringConfigureCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678c4";
    public override string Name => "configure";
    public override string Description => "Configures diagnostics settings for a backup vault.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceId);
    }

    protected override MonitoringConfigureOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.LogAnalyticsWorkspaceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceId.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.ConfigureMonitoringAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.VaultType, options.LogAnalyticsWorkspaceId, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new MonitoringConfigureCommandResult(result), AzureBackupJsonContext.Default.MonitoringConfigureCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error configuring monitoring"); HandleException(context, ex); }
        return context.Response;
    }

    internal record MonitoringConfigureCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
