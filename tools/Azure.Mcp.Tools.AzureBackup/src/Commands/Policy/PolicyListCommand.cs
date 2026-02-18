// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Options.Policy;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.AzureBackup.Commands.Policy;

public sealed class PolicyListCommand(ILogger<PolicyListCommand> logger) : BaseAzureBackupCommand<PolicyListOptions>()
{
    private const string CommandTitle = "List Backup Policies";
    private readonly ILogger<PolicyListCommand> _logger = logger;

    public override string Id => "b1a2c3d4-e5f6-7890-abcd-ef1234567893";
    public override string Name => "list";
    public override string Description =>
        """
        Lists all backup policies configured in the specified vault, including policy name,
        datasource types, and protected items count.
        """;
    public override string Title => CommandTitle;
    public override ToolMetadata Metadata => new()
    {
        Destructive = false, Idempotent = true, OpenWorld = false,
        ReadOnly = true, LocalRequired = false, Secret = false
    };

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
            var policies = await service.ListPoliciesAsync(
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.VaultType,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new PolicyListCommandResult(policies),
                AzureBackupJsonContext.Default.PolicyListCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing policies. Vault: {Vault}", options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record PolicyListCommandResult([property: JsonPropertyName("policies")] List<BackupPolicyInfo> Policies);
}
