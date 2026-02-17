// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Options;
using Azure.Mcp.Tools.DataProtection.Options.Job;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.DataProtection.Commands.Job;

public sealed class JobGetCommand(ILogger<JobGetCommand> logger) : BaseVaultCommand<JobGetOptions>()
{
    private readonly ILogger<JobGetCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f57";

    public override string Name => "get";

    public override string Description =>
        "Gets details of a specific backup job in an Azure Backup vault, including operation, status, data source, and timing information.";

    public override string Title => "Get Backup Job";

    public override ToolMetadata Metadata => new()
    {
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        LocalRequired = false,
        Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(DataProtectionOptionDefinitions.Job.AsRequired());
    }

    protected override JobGetOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Job = parseResult.GetValueOrDefault<string>(DataProtectionOptionDefinitions.Job.Name);
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
            var service = context.GetService<IDataProtectionService>();
            var job = await service.GetJobAsync(
                options.Job!,
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new JobGetCommandResult(job),
                DataProtectionJsonContext.Default.JobGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup job. Job: {Job}, Vault: {Vault}", options.Job, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record JobGetCommandResult(BackupJobModel Job);
}
