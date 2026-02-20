// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.Bulk;

public class BulkEnableOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.VaultName)]
    public string? Vault { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.VaultTypeName)]
    public string? VaultType { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.WorkloadTypeName)]
    public string? WorkloadType { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.PolicyName)]
    public string? Policy { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ResourceGroupFilterName)]
    public string? ResourceGroupFilter { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TagFilterName)]
    public string? TagFilter { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ResourceIdsName)]
    public string? ResourceIds { get; set; }
}
