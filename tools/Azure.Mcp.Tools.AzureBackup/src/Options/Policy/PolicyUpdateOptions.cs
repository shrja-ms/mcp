// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Policy;

public class PolicyUpdateOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.PolicyName)]
    public string? Policy { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ScheduleFrequencyName)]
    public string? ScheduleFrequency { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ScheduleTimeName)]
    public string? ScheduleTime { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.DailyRetentionDaysName)]
    public string? DailyRetentionDays { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.WeeklyRetentionWeeksName)]
    public string? WeeklyRetentionWeeks { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.MonthlyRetentionMonthsName)]
    public string? MonthlyRetentionMonths { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.YearlyRetentionYearsName)]
    public string? YearlyRetentionYears { get; set; }
}
