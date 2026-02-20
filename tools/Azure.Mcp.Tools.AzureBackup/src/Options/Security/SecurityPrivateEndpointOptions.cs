// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Security;

public class SecurityPrivateEndpointOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.VnetIdName)]
    public string? VnetId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.SubnetIdName)]
    public string? SubnetId { get; set; }
}
