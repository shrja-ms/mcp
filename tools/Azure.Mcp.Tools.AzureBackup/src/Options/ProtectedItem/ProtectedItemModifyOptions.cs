// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.ProtectedItem;

public class ProtectedItemModifyOptions : BaseProtectedItemOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.NewPolicyNameName)]
    public string? NewPolicyName { get; set; }
}
