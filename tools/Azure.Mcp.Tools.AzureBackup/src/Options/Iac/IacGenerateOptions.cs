// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.AzureBackup.Options.Iac;

public class IacGenerateOptions : BaseAzureBackupOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.IacFormatName)]
    public string? IacFormat { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.IncludeProtectedItemsName)]
    public string? IncludeProtectedItems { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.IncludeRbacName)]
    public string? IncludeRbac { get; set; }
}
