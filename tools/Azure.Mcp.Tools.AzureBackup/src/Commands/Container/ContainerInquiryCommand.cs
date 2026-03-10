// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Container;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Container;

public sealed class ContainerInquiryCommand(ILogger<ContainerInquiryCommand> logger) : BaseAzureBackupCommand<ContainerInquiryOptions>()
{
    private const string CommandTitle = "Trigger Inquiry";
    private readonly ILogger<ContainerInquiryCommand> _logger = logger;

    public override string Id => "c1a2b3c4-d5e6-7890-abcd-container00002";
    public override string Name => "inquiry";
    public override string Description =>
        """
        Triggers database discovery (inquiry) on a registered protection container in the Recovery Services vault.
        After registering a VM container, run inquiry to discover SQL or SAP HANA databases.
        Then use 'azurebackup protectableitem list' to see discovered databases.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = false, Idempotent = true, OpenWorld = false,
        ReadOnly = false, LocalRequired = false, Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Container);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
    }

    protected override ContainerInquiryOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Container = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Container.Name);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
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
            var result = await service.TriggerInquiryAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Container!,
                options.WorkloadType,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new ContainerInquiryCommandResult(result),
                AzureBackupJsonContext.Default.ContainerInquiryCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering inquiry. Container: {Container}, Vault: {Vault}",
                options.Container, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        ArgumentException argEx => argEx.Message,
        RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    internal record ContainerInquiryCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
