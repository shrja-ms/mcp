// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.RecoveryPoint;

public class RecoveryPointArchiveOptions : BaseProtectedItemOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.RecoveryPointName)]
    public string? RecoveryPoint { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.CheckEligibilityOnlyName)]
    public string? CheckEligibilityOnly { get; set; }
}
