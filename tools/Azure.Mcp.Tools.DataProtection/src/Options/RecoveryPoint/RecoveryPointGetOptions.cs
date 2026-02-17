// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.DataProtection.Options.BackupInstance;

namespace Azure.Mcp.Tools.DataProtection.Options.RecoveryPoint;

public class RecoveryPointGetOptions : BackupInstanceGetOptions
{
    [JsonPropertyName(DataProtectionOptionDefinitions.RecoveryPointName)]
    public string? RecoveryPoint { get; set; }
}
