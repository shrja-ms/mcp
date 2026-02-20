// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Diagnostics;

public class DiagnosticsDiagnoseOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.JobName)]
    public string? Job { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.DatasourceIdName)]
    public string? DatasourceId { get; set; }
}
