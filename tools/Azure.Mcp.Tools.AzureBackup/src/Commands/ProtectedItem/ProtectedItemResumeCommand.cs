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

public sealed class ProtectedItemResumeCommand(ILogger<ProtectedItemResumeCommand> logger) : BaseProtectedItemCommand<ProtectedItemResumeOptions>()
{
    private const string CommandTitle = "Resume Protection";
    private readonly ILogger<ProtectedItemResumeCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678b1";
    public override string Name => "resume";
    public override string Description => "Resumes protection for a previously stopped backup item.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.Policy);
    }

    protected override ProtectedItemResumeOptions BindOptions(ParseResult parseResult)
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
            var result = await service.ResumeProtectionAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.ProtectedItem!, options.VaultType, options.Container, options.Policy, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new ProtectedItemResumeCommandResult(result), AzureBackupJsonContext.Default.ProtectedItemResumeCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error resuming protection"); HandleException(context, ex); }
        return context.Response;
    }

    internal record ProtectedItemResumeCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
