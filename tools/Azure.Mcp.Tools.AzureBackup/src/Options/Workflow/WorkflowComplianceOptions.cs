// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.Workflow;

public class WorkflowComplianceOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.ResourceGroupFilterName)]
    public string? ResourceGroupFilter { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ResourceTypeFilterName)]
    public string? ResourceTypeFilter { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TagFilterName)]
    public string? TagFilter { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.VaultName)]
    public string? Vault { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.PolicyName)]
    public string? Policy { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.AutoRemediateName)]
    public string? AutoRemediate { get; set; }
}
