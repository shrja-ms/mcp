// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;

public sealed class ProtectedItemModifyCommand(ILogger<ProtectedItemModifyCommand> logger) : BaseProtectedItemCommand<ProtectedItemModifyOptions>()
{
    private const string CommandTitle = "Modify Protection Policy";
    private readonly ILogger<ProtectedItemModifyCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678b2";
    public override string Name => "modify";
    public override string Description => "Switches the backup policy associated with a protected item.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.NewPolicyName);
    }

    protected override ProtectedItemModifyOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.NewPolicyName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.NewPolicyName.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.ModifyProtectionAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.ProtectedItem!, options.VaultType, options.Container, options.NewPolicyName, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new ProtectedItemModifyCommandResult(result), AzureBackupJsonContext.Default.ProtectedItemModifyCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error modifying protection"); HandleException(context, ex); }
        return context.Response;
    }

    internal record ProtectedItemModifyCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
