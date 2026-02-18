// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options.Job;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Job;

public sealed class JobListCommand(ILogger<JobListCommand> logger) : BaseAzureBackupCommand<JobListOptions>()
{
    private const string CommandTitle = "List Backup Jobs";
    private readonly ILogger<JobListCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef1234567895";
    public override string Name => "list";
    public override string Description =>
        """
        Lists all backup jobs in the specified vault, including operation type, status,
        start/end times, and datasource information.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = false, Idempotent = true, OpenWorld = false,
        ReadOnly = true, LocalRequired = false, Secret = false
    };

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid)
        {
            return context.Response;
        }

        var options = BindOptions(parseResult);

        try
        {
            var service = context.GetService<IAzureBackupService>();
            var jobs = await service.ListJobsAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new JobListCommandResult(jobs),
                AzureBackupJsonContext.Default.JobListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing jobs. Vault: {Vault}", options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record JobListCommandResult([property: JsonPropertyName("jobs")] List<BackupJobInfo> Jobs);
}
