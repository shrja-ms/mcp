// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.ProtectedItem;

public class ProtectedItemAutoProtectOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.VaultName)]
    public string? Vault { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.VmResourceIdName)]
    public string? VmResourceId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.InstanceNameName)]
    public string? InstanceName { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.PolicyName)]
    public string? Policy { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.WorkloadTypeName)]
    public string? WorkloadType { get; set; }
}
