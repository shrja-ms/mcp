// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.Workflow;

public class WorkflowMigrateOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.SourceVaultNameName)]
    public string? SourceVaultName { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.VaultTypeName)]
    public string? VaultType { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ResourceGroupFilterName)]
    public string? ResourceGroupFilter { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetResourceIdName)]
    public string? TargetResourceId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetVaultNameName)]
    public string? TargetVaultName { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.RestoreLocationName)]
    public string? RestoreLocation { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ForceName)]
    public string? Force { get; set; }
}
