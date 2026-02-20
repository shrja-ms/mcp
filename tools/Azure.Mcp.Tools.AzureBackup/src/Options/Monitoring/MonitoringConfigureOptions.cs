// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Monitoring;

public class MonitoringConfigureOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceIdName)]
    public string? LogAnalyticsWorkspaceId { get; set; }
}
