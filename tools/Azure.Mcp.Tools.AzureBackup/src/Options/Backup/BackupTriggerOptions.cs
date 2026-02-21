// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Backup;

public class BackupTriggerOptions : BaseProtectedItemOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.ExpiryName)]
    public string? Expiry { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.BackupTypeName)]
    public string? BackupType { get; set; }
}
