// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Iac;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Iac;

public sealed class IacGenerateCommand(ILogger<IacGenerateCommand> logger) : BaseAzureBackupCommand<IacGenerateOptions>()
{
    private const string CommandTitle = "Generate IaC Template";
    private readonly ILogger<IacGenerateCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678ee";
    public override string Name => "generate";
    public override string Description => "Generates Terraform or Bicep templates from existing vault configuration.";
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new() { Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, LocalRequired = false, Secret = false };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.IacFormat);
        command.Options.Add(AzureBackupOptionDefinitions.IncludeProtectedItems);
        command.Options.Add(AzureBackupOptionDefinitions.IncludeRbac);
    }

    protected override IacGenerateOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.IacFormat = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.IacFormat.Name);
        options.IncludeProtectedItems = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.IncludeProtectedItems.Name);
        options.IncludeRbac = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.IncludeRbac.Name);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid) return context.Response;
        var options = BindOptions(parseResult);
        try
        {
            var service = context.GetService<IAzureBackupService>();
            var includeItems = string.Equals(options.IncludeProtectedItems, "true", StringComparison.OrdinalIgnoreCase);
            var includeRbac = string.Equals(options.IncludeRbac, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.GenerateIacFromVaultAsync(options.Vault!, options.ResourceGroup!, options.Subscription!, options.IacFormat ?? "terraform", options.VaultType, includeItems, includeRbac, options.Tenant, options.RetryPolicy, cancellationToken);
            context.Response.Results = ResponseResult.Create(new IacGenerateCommandResult(result), AzureBackupJsonContext.Default.IacGenerateCommandResult);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error generating IaC template"); HandleException(context, ex); }
        return context.Response;
    }

    internal record IacGenerateCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
