// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Mcp.Tools.DataProtection.Options.Job;

public class JobGetOptions : BaseDataProtectionOptions
{
    [JsonPropertyName(DataProtectionOptionDefinitions.JobName)]
    public string? Job { get; set; }
}
