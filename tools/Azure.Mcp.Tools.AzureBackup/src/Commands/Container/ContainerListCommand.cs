// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options.Container;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Container;

public sealed class ContainerListCommand(ILogger<ContainerListCommand> logger) : BaseAzureBackupCommand<ContainerListOptions>()
{
    private const string CommandTitle = "List Containers";
    private readonly ILogger<ContainerListCommand> _logger = logger;

    public override string Id => "c1a2b3c4-d5e6-7890-abcd-container00003";
    public override string Name => "list";
    public override string Description =>
        """
        Lists all protection containers registered in the Recovery Services vault, including
        their registration status, health status, container type, and source VM information.
        Use this to verify container registration progress after calling 'azurebackup container register'.
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
            var containers = await service.ListContainersAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new ContainerListCommandResult(containers),
                AzureBackupJsonContext.Default.ContainerListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing containers. Vault: {Vault}", options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record ContainerListCommandResult([property: JsonPropertyName("containers")] List<ContainerInfo> Containers);
}
