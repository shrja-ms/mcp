// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Extensions;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Options;
using Azure.Mcp.Tools.DataProtection.Options.Policy;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;

namespace Azure.Mcp.Tools.DataProtection.Commands.Policy;

public sealed class PolicyGetCommand(ILogger<PolicyGetCommand> logger) : BaseVaultCommand<PolicyGetOptions>()
{
    private readonly ILogger<PolicyGetCommand> _logger = logger;

    public override string Id => "d0e1f2a3-b4c5-6d7e-8f9a-0b1c2d3e4f55";

    public override string Name => "get";

    public override string Description =>
        "Gets details of a specific backup policy in an Azure Backup vault, including data source type and data store configurations.";

    public override string Title => "Get Backup Policy";

    public override ToolMetadata Metadata => new()
    {
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        LocalRequired = false,
        Secret = false
    };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(DataProtectionOptionDefinitions.Policy.AsRequired());
    }

    protected override PolicyGetOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Policy = parseResult.GetValueOrDefault<string>(DataProtectionOptionDefinitions.Policy.Name);
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
            var service = context.GetService<IDataProtectionService>();
            var policy = await service.GetPolicyAsync(
                options.Policy!,
                options.Vault!,
                options.ResourceGroup!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new PolicyGetCommandResult(policy),
                DataProtectionJsonContext.Default.PolicyGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup policy. Policy: {Policy}, Vault: {Vault}", options.Policy, options.Vault);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record PolicyGetCommandResult(BackupPolicyModel Policy);
}
