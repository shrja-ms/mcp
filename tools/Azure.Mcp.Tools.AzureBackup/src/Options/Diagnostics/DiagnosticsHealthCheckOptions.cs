// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Diagnostics;

public class DiagnosticsHealthCheckOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.RpoThresholdHoursName)]
    public string? RpoThresholdHours { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.IncludeSecurityPostureName)]
    public string? IncludeSecurityPosture { get; set; }
}
