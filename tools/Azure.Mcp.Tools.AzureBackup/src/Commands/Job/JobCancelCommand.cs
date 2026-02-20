// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Job;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Job;

public sealed class JobCancelCommand(ILogger<JobCancelCommand> logger) : BaseAzureBackupCommand<JobCancelOptions>()
{
    private const string CommandTitle = "Cancel Backup Job";
    private readonly ILogger<JobCancelCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678b6";
    public override string Name => "cancel";
    public override string Description => "Cancels a running backup or restore job.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Job);
    }

    protected override JobCancelOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Job = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Job.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.CancelJobAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.Job!, options.VaultType, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new JobCancelCommandResult(result), AzureBackupJsonContext.Default.JobCancelCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling job"); HandleException(context, ex); }
        return context.Response;
    }

    internal record JobCancelCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
