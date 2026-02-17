// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.DataProtection.Options.RecoveryPoint;

public class RecoveryPointListOptions : BaseDataProtectionOptions
{
    [JsonPropertyName(DataProtectionOptionDefinitions.BackupInstanceName)]
    public string? BackupInstance { get; set; }
}
