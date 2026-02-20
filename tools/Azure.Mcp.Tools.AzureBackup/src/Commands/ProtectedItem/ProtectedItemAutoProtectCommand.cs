// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Models.Option;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;

public sealed class ProtectedItemAutoProtectCommand(ILogger<ProtectedItemAutoProtectCommand> logger) : SubscriptionCommand<ProtectedItemAutoProtectOptions>()
{
    private const string CommandTitle = "Enable Auto-Protection";
    private readonly ILogger<ProtectedItemAutoProtectCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678b4";
    public override string Name => "autoprotect";
    public override string Description => "Enables auto-protection for SQL/HANA databases on a VM.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Vault);
        command.Options.Add(AzureBackupOptionDefinitions.VmResourceId);
        command.Options.Add(AzureBackupOptionDefinitions.InstanceName);
        command.Options.Add(AzureBackupOptionDefinitions.Policy);
        command.Options.Add(AzureBackupOptionDefinitions.WorkloadType);
    }

    protected override ProtectedItemAutoProtectOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Vault = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Vault.Name);
        options.VmResourceId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.VmResourceId.Name);
        options.InstanceName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.InstanceName.Name);
        options.Policy = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Policy.Name);
        options.WorkloadType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.WorkloadType.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.EnableAutoProtectionAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.VmResourceId!, options.InstanceName!, options.Policy!, options.WorkloadType!, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new ProtectedItemAutoProtectCommandResult(result), AzureBackupJsonContext.Default.ProtectedItemAutoProtectCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error enabling auto-protection"); HandleException(context, ex); }
        return context.Response;
    }

    internal record ProtectedItemAutoProtectCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
