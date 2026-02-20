// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.Monitoring;

public class MonitoringReportsOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.ReportTypeName)]
    public string? ReportType { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.LogAnalyticsWorkspaceIdName)]
    public string? LogAnalyticsWorkspaceId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TimeRangeDaysName)]
    public string? TimeRangeDays { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.WorkloadTypeName)]
    public string? WorkloadType { get; set; }
}
