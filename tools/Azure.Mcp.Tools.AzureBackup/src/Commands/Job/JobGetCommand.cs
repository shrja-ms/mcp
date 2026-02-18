// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Job;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Job;

public sealed class JobGetCommand(ILogger<JobGetCommand> logger) : BaseAzureBackupCommand<JobGetOptions>()
{
    private const string CommandTitle = "Get Backup Job";
    private readonly ILogger<JobGetCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef1234567896";
    public override string Name => "get";
    public override string Description =>
        """
        Retrieves detailed information about a specific backup job, including operation type,
        status, start/end times, and datasource details.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = false, Idempotent = true, OpenWorld = false,
        ReadOnly = true, LocalRequired = false, Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Job);
    }

    protected override JobGetOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Job = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Job.Name);
        return options;
    }

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
            var job = await service.GetJobAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Job!,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new JobGetCommandResult(job),
                AzureBackupJsonContext.Default.JobGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job. Job: {Job}, Vault: {Vault}", options.Job, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        RequestFailedException reqEx when reqEx.Status == (int)HttpStatusCode.NotFound =>
            "Job not found. Verify the job ID and vault.",
        RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    internal record JobGetCommandResult([property: JsonPropertyName("job")] BackupJobInfo Job);
}
