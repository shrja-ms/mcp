// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Options.Job;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.DataProtection.Commands.Job;

public sealed class JobListCommand(ILogger<JobListCommand> logger) : BaseVaultCommand<JobListOptions>()
{
    private readonly ILogger<JobListCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f56";

    public override string Name => "list";

    public override string Description =>
        "Lists all backup jobs in an Azure Backup vault. Returns job name, operation, status, data source, and timing information.";

    public override string Title => "List Backup Jobs";

    public override ToolMetadata Metadata => new()
    {
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        LocalRequired = false,
        Secret = false
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
            var service = context.GetService<IDataProtectionService>();
            var jobs = await service.ListJobsAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new JobListCommandResult(jobs?.ToList() ?? []),
                DataProtectionJsonContext.Default.JobListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backup jobs. Vault: {Vault}", options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record JobListCommandResult(List<BackupJobModel> Jobs);
}
