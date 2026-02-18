// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Restore;

public class RestoreTriggerOptions : BaseProtectedItemOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.RecoveryPointName)]
    public string? RecoveryPoint { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetResourceIdName)]
    public string? TargetResourceId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.RestoreLocationName)]
    public string? RestoreLocation { get; set; }
}
