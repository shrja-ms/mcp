// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options;
using Azure.Mcp.Tools.AzureBackup.Options.Governance;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Governance;

public sealed class GovernanceApplyPolicyCommand(ILogger<GovernanceApplyPolicyCommand> logger) : SubscriptionCommand<GovernanceApplyPolicyOptions>()
{
    private const string CommandTitle = "Apply Azure Backup Policy";
    private readonly ILogger<GovernanceApplyPolicyCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef12345678c9";
    public override string Name => "apply-policy";
    public override string Description =>
        """
        Assigns an Azure Policy definition for backup governance at the specified scope.
        Optionally deploys a remediation task to bring existing resources into compliance.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = true, Idempotent = true, OpenWorld = false,
        ReadOnly = false, LocalRequired = false, Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(AzureBackupOptionDefinitions.PolicyDefinitionId);
        command.Options.Add(AzureBackupOptionDefinitions.Scope);
        command.Options.Add(AzureBackupOptionDefinitions.DeployRemediation);
    }

    protected override GovernanceApplyPolicyOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.PolicyDefinitionId = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.PolicyDefinitionId.Name);
        options.Scope = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.Scope.Name);
        options.DeployRemediation = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.DeployRemediation.Name);
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
            var deployRemediation = string.Equals(options.DeployRemediation, "true", StringComparison.OrdinalIgnoreCase);
            var result = await service.ApplyAzurePolicyAsync(
                options.PolicyDefinitionId!,
                options.Scope!,
                null,
                deployRemediation,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new GovernanceApplyPolicyCommandResult(result),
                AzureBackupJsonContext.Default.GovernanceApplyPolicyCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying Azure Policy. PolicyDefinitionId: {PolicyDefinitionId}, Scope: {Scope}",
                options.PolicyDefinitionId, options.Scope);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record GovernanceApplyPolicyCommandResult([property: JsonPropertyName("result")] OperationResult Result);
}
