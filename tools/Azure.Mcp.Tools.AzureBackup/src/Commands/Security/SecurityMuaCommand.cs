// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Security;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Security;

public sealed class SecurityMuaCommand(ILogger<SecurityMuaCommand> logger) : BaseAzureBackupCommand<SecurityMuaOptions>()
{
    private const string CommandTitle = "Configure Multi-User Authorization";
    private readonly ILogger<SecurityMuaCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678c1";
    public override string Name => "mua";
    public override string Description => "Links a vault to a Resource Guard for multi-user authorization.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.ResourceGuardId);
    }

    protected override SecurityMuaOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGuardId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.ResourceGuardId.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.ConfigureMuaAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.ResourceGuardId!, options.VaultType, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new SecurityMuaCommandResult(result), AzureBackupJsonContext.Default.SecurityMuaCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error configuring MUA"); HandleException(context, ex); }
        return context.Response;
    }

    internal record SecurityMuaCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
