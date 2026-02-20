// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Dr;

public class DrCrossRegionRestoreOptions : BaseProtectedItemOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.RecoveryPointName)]
    public string? RecoveryPointId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.RestoreModeName)]
    public string? RestoreMode { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.SecondaryRegionName)]
    public string? SecondaryRegion { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetResourceIdName)]
    public string? TargetResourceId { get; set; }
}
