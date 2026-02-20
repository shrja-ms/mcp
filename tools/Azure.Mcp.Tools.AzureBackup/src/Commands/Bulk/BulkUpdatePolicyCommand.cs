// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Bulk;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Bulk;

public sealed class BulkUpdatePolicyCommand(ILogger<BulkUpdatePolicyCommand> logger) : BaseAzureBackupCommand<BulkUpdatePolicyOptions>()
{
    private const string CommandTitle = "Bulk Update Policy";
    private readonly ILogger<BulkUpdatePolicyCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678ed";
    public override string Name => "updatepolicy";
    public override string Description => "Switches all items from one policy to another in a vault.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.SourcePolicyName);
        command.Options.Add(AzureBackupOptionDefinitions.TargetPolicyName);
    }

    protected override BulkUpdatePolicyOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.SourcePolicyName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.SourcePolicyName.Name);
        options.TargetPolicyName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.TargetPolicyName.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.BulkUpdatePolicyAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.SourcePolicyName!, options.TargetPolicyName!, options.VaultType, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new BulkUpdatePolicyCommandResult(result), AzureBackupJsonContext.Default.BulkUpdatePolicyCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error bulk updating policy"); HandleException(context, ex); }
        return context.Response;
    }

    internal record BulkUpdatePolicyCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
