// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Mcp.Tools.AzureBackup.Commands.Backup;
using Azure.Mcp.Tools.AzureBackup.Commands.Job;
using Azure.Mcp.Tools.AzureBackup.Commands.Policy;
using Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Commands.RecoveryPoint;
using Azure.Mcp.Tools.AzureBackup.Commands.Restore;
using Azure.Mcp.Tools.AzureBackup.Commands.Vault;
using Azure.Mcp.Tools.AzureBackup.Models;

namespace Azure.Mcp.Tools.AzureBackup.Commands;

[JsonSerializable(typeof(VaultListCommand.VaultListCommandResult))]
[JsonSerializable(typeof(VaultGetCommand.VaultGetCommandResult))]
[JsonSerializable(typeof(VaultCreateCommand.VaultCreateCommandResult))]
[JsonSerializable(typeof(PolicyListCommand.PolicyListCommandResult))]
[JsonSerializable(typeof(PolicyGetCommand.PolicyGetCommandResult))]
[JsonSerializable(typeof(JobListCommand.JobListCommandResult))]
[JsonSerializable(typeof(JobGetCommand.JobGetCommandResult))]
[JsonSerializable(typeof(RecoveryPointListCommand.RecoveryPointListCommandResult))]
[JsonSerializable(typeof(RecoveryPointGetCommand.RecoveryPointGetCommandResult))]
[JsonSerializable(typeof(ProtectedItemListCommand.ProtectedItemListCommandResult))]
[JsonSerializable(typeof(ProtectedItemGetCommand.ProtectedItemGetCommandResult))]
[JsonSerializable(typeof(ProtectedItemProtectCommand.ProtectedItemProtectCommandResult))]
[JsonSerializable(typeof(BackupTriggerCommand.BackupTriggerCommandResult))]
[JsonSerializable(typeof(RestoreTriggerCommand.RestoreTriggerCommandResult))]
[JsonSerializable(typeof(BackupVaultInfo))]
[JsonSerializable(typeof(ProtectedItemInfo))]
[JsonSerializable(typeof(BackupPolicyInfo))]
[JsonSerializable(typeof(BackupJobInfo))]
[JsonSerializable(typeof(RecoveryPointInfo))]
[JsonSerializable(typeof(VaultCreateResult))]
[JsonSerializable(typeof(ProtectResult))]
[JsonSerializable(typeof(BackupTriggerResult))]
[JsonSerializable(typeof(RestoreTriggerResult))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
internal sealed partial class AzureBackupJsonContext : JsonSerializerContext
{
}
