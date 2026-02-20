// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Policy;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Policy;

public sealed class PolicyDeleteCommand(ILogger<PolicyDeleteCommand> logger) : BaseAzureBackupCommand<PolicyDeleteOptions>()
{
    private const string CommandTitle = "Delete Backup Policy";
    private readonly ILogger<PolicyDeleteCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678a4";
    public override string Name => "delete";
    public override string Description => "Deletes a backup policy after verifying no protected items are associated with it.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Policy);
    }

    protected override PolicyDeleteOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Policy = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Policy.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.DeletePolicyAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.Policy!, options.VaultType, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new PolicyDeleteCommandResult(result), AzureBackupJsonContext.Default.PolicyDeleteCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting policy"); HandleException(context, ex); }
        return context.Response;
    }

    internal record PolicyDeleteCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
