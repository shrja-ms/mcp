// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.Workflow;

public class WorkflowSetupSqlHanaOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.VmResourceIdName)]
    public string? VmResourceId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.WorkloadTypeName)]
    public string? WorkloadType { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.LocationName)]
    public string? Location { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.VaultName)]
    public string? Vault { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.AutoProtectName)]
    public string? AutoProtect { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.OutputIacName)]
    public string? OutputIac { get; set; }
}
