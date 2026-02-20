// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.Workflow;

public class WorkflowSetupVmOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.ResourceIdsName)]
    public string? ResourceIds { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.LocationName)]
    public string? Location { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.VaultName)]
    public string? Vault { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ScheduleFrequencyName)]
    public string? ScheduleFrequency { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.DailyRetentionDaysName)]
    public string? DailyRetentionDays { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.TriggerFirstBackupName)]
    public string? TriggerFirstBackup { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.OutputIacName)]
    public string? OutputIac { get; set; }
}
