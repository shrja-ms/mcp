// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Cost;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Cost;

public sealed class CostEstimateCommand(ILogger<CostEstimateCommand> logger) : BaseAzureBackupCommand<CostEstimateOptions>()
{
    private const string CommandTitle = "Estimate Backup Cost";
    private readonly ILogger<CostEstimateCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678d7";
    public override string Name => "estimate";
    public override string Description => "Estimates monthly backup cost for a vault based on current usage.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
        command.Options.Add(AzureBackupOptionDefinitions.IncludeArchiveProjection);
    }

    protected override CostEstimateOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
        options.IncludeArchiveProjection = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.IncludeArchiveProjection.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var includeArchive = string.Equals(options.IncludeArchiveProjection, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.EstimateBackupCostAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.VaultType, options.WorkloadType, includeArchive, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new CostEstimateCommandResult(result), AzureBackupJsonContext.Default.CostEstimateCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error estimating cost"); HandleException(context, ex); }
        return context.Response;
    }

    internal record CostEstimateCommandResult([property: JsonPropertyName("estimate")] CostEstimateResult Estimate);
}
