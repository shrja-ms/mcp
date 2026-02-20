// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Bulk;

public class BulkUpdatePolicyOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.SourcePolicyNameName)]
    public string? SourcePolicyName { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetPolicyNameName)]
    public string? TargetPolicyName { get; set; }
}
