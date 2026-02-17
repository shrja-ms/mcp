// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Core.Options;

namespace Azure.Mcp.Tools.DataProtection.Options;

public class BaseDataProtectionOptions : SubscriptionOptions
{
    [JsonPropertyName(DataProtectionOptionDefinitions.VaultName)]
    public string? Vault { get; set; }
}
