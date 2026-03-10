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

public sealed class ContainerRegisterCommand(ILogger<ContainerRegisterCommand> logger) : BaseAzureBackupCommand<ContainerRegisterOptions>()
{
    private const string CommandTitle = "Register Container";
    private readonly ILogger<ContainerRegisterCommand> _logger = logger;

    public override string Id => "c1a2b3c4-d5e6-7890-abcd-container00001";
    public override string Name => "register";
    public override string Description =>
        """
        Registers a VM as a protection container in the Recovery Services vault for SQL or SAP HANA workload backup.
        This is the first step for workload backup: register the VM, then trigger inquiry to discover databases,
        then protect individual databases. Requires the VM ARM resource ID and workload type (SQLDataBase or SAPHana).
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
        command.Options.Add(AzureBackupOptionDefinitions.VmResourceId);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
    }

    protected override ContainerRegisterOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.VmResourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.VmResourceId.Name);
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
            var result = await service.RegisterContainerAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.VmResourceId!,
                options.WorkloadType!,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new ContainerRegisterCommandResult(result),
                AzureBackupJsonContext.Default.ContainerRegisterCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering container. VM: {VmResourceId}, Vault: {Vault}",
                options.VmResourceId, options.Vault);
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

    internal record ContainerRegisterCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
