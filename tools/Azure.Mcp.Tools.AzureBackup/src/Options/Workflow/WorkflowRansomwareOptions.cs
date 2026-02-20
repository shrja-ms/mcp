// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Workflow;

public class WorkflowRansomwareOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.ResourceIdsName)]
    public string? ResourceIds { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.InfectionTimestampName)]
    public string? InfectionTimestamp { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ForceName)]
    public string? Force { get; set; }
}
