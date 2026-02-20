// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.ProtectedItem;

public class ProtectedItemStopOptions : BaseProtectedItemOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.ModeName)]
    public string? Mode { get; set; }
}
