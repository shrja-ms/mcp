// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AzureBackup.Options.Security;

public class SecurityRbacOptions : SubscriptionOptions
{
    [JsonPropertyName(AzureBackupOptionDefinitions.PrincipalIdName)]
    public string? PrincipalId { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.RoleNameName)]
    public string? RoleName { get; set; }

    [JsonPropertyName(AzureBackupOptionDefinitions.ScopeName)]
    public string? Scope { get; set; }
}
