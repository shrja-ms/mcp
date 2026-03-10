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

    [JsonPropertyName(AzureBackupOptionDefinitions.StagingStorageAccountIdName)]
    public string? StagingStorageAccountId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.PointInTimeName)]
    public string? PointInTime { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.RestoreModeName)]
    public string? RestoreMode { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetVmNameName)]
    public string? TargetVmName { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetVnetIdName)]
    public string? TargetVnetId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetSubnetIdName)]
    public string? TargetSubnetId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetDatabaseNameName)]
    public string? TargetDatabaseName { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TargetInstanceNameName)]
    public string? TargetInstanceName { get; set; }
}
