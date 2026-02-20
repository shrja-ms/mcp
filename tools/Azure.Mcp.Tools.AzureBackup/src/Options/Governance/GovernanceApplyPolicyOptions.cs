// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.Governance;

public class GovernanceApplyPolicyOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.PolicyDefinitionIdName)]
    public string? PolicyDefinitionId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ScopeName)]
    public string? Scope { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.DeployRemediationName)]
    public string? DeployRemediation { get; set; }
}
