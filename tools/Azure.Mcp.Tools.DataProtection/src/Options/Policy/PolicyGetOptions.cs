// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.DataProtection.Options.Policy;

public class PolicyGetOptions : BaseDataProtectionOptions
{
    [JsonPropertyName(DataProtectionOptionDefinitions.PolicyName)]
    public string? Policy { get; set; }
}
