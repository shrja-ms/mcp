// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.DataProtection.Commands.BackupInstance;
using Azure.Mcp.Tools.DataProtection.Commands.Job;
using Azure.Mcp.Tools.DataProtection.Commands.Policy;
using Azure.Mcp.Tools.DataProtection.Commands.RecoveryPoint;
using Azure.Mcp.Tools.DataProtection.Commands.Vault;

namespace Azure.Mcp.Tools.DataProtection.Commands;

[JsonSerializable(typeof(VaultListCommand.VaultListCommandResult))]
[JsonSerializable(typeof(VaultGetCommand.VaultGetCommandResult))]
[JsonSerializable(typeof(BackupInstanceListCommand.BackupInstanceListCommandResult))]
[JsonSerializable(typeof(BackupInstanceGetCommand.BackupInstanceGetCommandResult))]
[JsonSerializable(typeof(PolicyListCommand.PolicyListCommandResult))]
[JsonSerializable(typeof(PolicyGetCommand.PolicyGetCommandResult))]
[JsonSerializable(typeof(JobListCommand.JobListCommandResult))]
[JsonSerializable(typeof(JobGetCommand.JobGetCommandResult))]
[JsonSerializable(typeof(RecoveryPointListCommand.RecoveryPointListCommandResult))]
[JsonSerializable(typeof(RecoveryPointGetCommand.RecoveryPointGetCommandResult))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
internal sealed partial class DataProtectionJsonContext : JsonSerializerContext;
