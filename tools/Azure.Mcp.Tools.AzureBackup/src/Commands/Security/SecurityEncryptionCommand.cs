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

public sealed class SecurityEncryptionCommand(ILogger<SecurityEncryptionCommand> logger) : BaseAzureBackupCommand<SecurityEncryptionOptions>()
{
    private const string CommandTitle = "Configure Vault Encryption";
    private readonly ILogger<SecurityEncryptionCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678c3";
    public override string Name => "encryption";
    public override string Description => "Configures customer-managed key encryption for a backup vault.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.KeyVaultUri);
        command.Options.Add(AzureBackupOptionDefinitions.KeyName);
        command.Options.Add(AzureBackupOptionDefinitions.IdentityType);
        command.Options.Add(AzureBackupOptionDefinitions.KeyVersion);
        command.Options.Add(AzureBackupOptionDefinitions.UserAssignedIdentityId);
    }

    protected override SecurityEncryptionOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.KeyVaultUri = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.KeyVaultUri.Name);
        options.KeyName = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.KeyName.Name);
        options.IdentityType = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.IdentityType.Name);
        options.KeyVersion = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.KeyVersion.Name);
        options.UserAssignedIdentityId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.UserAssignedIdentityId.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var result = await service.ConfigureEncryptionAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.KeyVaultUri!, options.KeyName!, options.IdentityType!, options.VaultType, options.KeyVersion, options.UserAssignedIdentityId, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new SecurityEncryptionCommandResult(result), AzureBackupJsonContext.Default.SecurityEncryptionCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error configuring encryption"); HandleException(context, ex); }
        return context.Response;
    }

    internal record SecurityEncryptionCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
