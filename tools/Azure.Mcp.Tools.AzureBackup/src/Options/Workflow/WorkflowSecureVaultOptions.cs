// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Workflow;

public class WorkflowSecureVaultOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.SecurityLevelName)]
    public string? SecurityLevel { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ResourceGuardIdName)]
    public string? ResourceGuardId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.KeyVaultUriName)]
    public string? KeyVaultUri { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.KeyNameName)]
    public string? KeyName { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceIdName)]
    public string? LogAnalyticsWorkspaceId { get; set; }
}
