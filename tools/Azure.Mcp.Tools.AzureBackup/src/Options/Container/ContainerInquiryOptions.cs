// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Container;

public class ContainerInquiryOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.ContainerName)]
    public string? Container { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.WorkloadTypeName)]
    public string? WorkloadType { get; set; }
}
