// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Core.Models.Option;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Security;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Security;

public sealed class SecurityRbacCommand(ILogger<SecurityRbacCommand> logger) : SubscriptionCommand<SecurityRbacOptions>()
{
    private const string CommandTitle = "Configure Backup RBAC";
    private readonly ILogger<SecurityRbacCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678c0";
    public override string Name => "rbac";
    public override string Description => "Assigns built-in backup RBAC roles to a principal at a given scope.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.PrincipalId);
        command.Options.Add(AzureBackupOptionDefinitions.RoleName);
        command.Options.Add(AzureBackupOptionDefinitions.Scope);
    }

    protected override SecurityRbacOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.PrincipalId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.PrincipalId.Name);
        options.RoleName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.RoleName.Name);
        options.Scope = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Scope.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.ConfigureRbacAsync(options.PrincipalId!, options.RoleName!, options.Scope!, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new SecurityRbacCommandResult(result), AzureBackupJsonContext.Default.SecurityRbacCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error configuring RBAC"); HandleException(context, ex); }
        return context.Response;
    }

    internal record SecurityRbacCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
