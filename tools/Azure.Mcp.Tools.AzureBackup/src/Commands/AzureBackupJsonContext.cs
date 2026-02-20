// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Mcp.Tools.AzureBackup.Commands.Backup;
using Azure.Mcp.Tools.AzureBackup.Commands.Bulk;
using Azure.Mcp.Tools.AzureBackup.Commands.Cost;
using Azure.Mcp.Tools.AzureBackup.Commands.Diagnostics;
using Azure.Mcp.Tools.AzureBackup.Commands.Dr;
using Azure.Mcp.Tools.AzureBackup.Commands.Governance;
using Azure.Mcp.Tools.AzureBackup.Commands.Iac;
using Azure.Mcp.Tools.AzureBackup.Commands.Job;
using Azure.Mcp.Tools.AzureBackup.Commands.Monitoring;
using Azure.Mcp.Tools.AzureBackup.Commands.Policy;
using Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Commands.RecoveryPoint;
using Azure.Mcp.Tools.AzureBackup.Commands.Restore;
using Azure.Mcp.Tools.AzureBackup.Commands.Security;
using Azure.Mcp.Tools.AzureBackup.Commands.Vault;
using Azure.Mcp.Tools.AzureBackup.Commands.Workflow;
using Azure.Mcp.Tools.AzureBackup.Models;

namespace Azure.Mcp.Tools.AzureBackup.Commands;

// Existing commands
[JsonSerializable(typeof(VaultListCommand.VaultListCommandResult))]
[JsonSerializable(typeof(VaultGetCommand.VaultGetCommandResult))]
[JsonSerializable(typeof(VaultCreateCommand.VaultCreateCommandResult))]
[JsonSerializable(typeof(VaultUpdateCommand.VaultUpdateCommandResult))]
[JsonSerializable(typeof(VaultDeleteCommand.VaultDeleteCommandResult))]
[JsonSerializable(typeof(PolicyListCommand.PolicyListCommandResult))]
[JsonSerializable(typeof(PolicyGetCommand.PolicyGetCommandResult))]
[JsonSerializable(typeof(PolicyCreateCommand.PolicyCreateCommandResult))]
[JsonSerializable(typeof(PolicyUpdateCommand.PolicyUpdateCommandResult))]
[JsonSerializable(typeof(PolicyDeleteCommand.PolicyDeleteCommandResult))]
[JsonSerializable(typeof(JobListCommand.JobListCommandResult))]
[JsonSerializable(typeof(JobGetCommand.JobGetCommandResult))]
[JsonSerializable(typeof(JobCancelCommand.JobCancelCommandResult))]
[JsonSerializable(typeof(RecoveryPointListCommand.RecoveryPointListCommandResult))]
[JsonSerializable(typeof(RecoveryPointGetCommand.RecoveryPointGetCommandResult))]
[JsonSerializable(typeof(RecoveryPointArchiveCommand.RecoveryPointArchiveCommandResult))]
[JsonSerializable(typeof(ProtectedItemListCommand.ProtectedItemListCommandResult))]
[JsonSerializable(typeof(ProtectedItemGetCommand.ProtectedItemGetCommandResult))]
[JsonSerializable(typeof(ProtectedItemProtectCommand.ProtectedItemProtectCommandResult))]
[JsonSerializable(typeof(ProtectedItemStopCommand.ProtectedItemStopCommandResult))]
[JsonSerializable(typeof(ProtectedItemResumeCommand.ProtectedItemResumeCommandResult))]
[JsonSerializable(typeof(ProtectedItemModifyCommand.ProtectedItemModifyCommandResult))]
[JsonSerializable(typeof(ProtectedItemUndeleteCommand.ProtectedItemUndeleteCommandResult))]
[JsonSerializable(typeof(ProtectedItemAutoProtectCommand.ProtectedItemAutoProtectCommandResult))]
[JsonSerializable(typeof(BackupTriggerCommand.BackupTriggerCommandResult))]
[JsonSerializable(typeof(BackupStatusCommand.BackupStatusCommandResult))]
[JsonSerializable(typeof(RestoreTriggerCommand.RestoreTriggerCommandResult))]
// Security
[JsonSerializable(typeof(SecurityRbacCommand.SecurityRbacCommandResult))]
[JsonSerializable(typeof(SecurityMuaCommand.SecurityMuaCommandResult))]
[JsonSerializable(typeof(SecurityPrivateEndpointCommand.SecurityPrivateEndpointCommandResult))]
[JsonSerializable(typeof(SecurityEncryptionCommand.SecurityEncryptionCommandResult))]
// Monitoring
[JsonSerializable(typeof(MonitoringConfigureCommand.MonitoringConfigureCommandResult))]
[JsonSerializable(typeof(MonitoringReportsCommand.MonitoringReportsCommandResult))]
// Governance
[JsonSerializable(typeof(GovernanceFindUnprotectedCommand.GovernanceFindUnprotectedCommandResult))]
[JsonSerializable(typeof(GovernanceApplyPolicyCommand.GovernanceApplyPolicyCommandResult))]
[JsonSerializable(typeof(GovernanceImmutabilityCommand.GovernanceImmutabilityCommandResult))]
[JsonSerializable(typeof(GovernanceSoftDeleteCommand.GovernanceSoftDeleteCommandResult))]
// DR
[JsonSerializable(typeof(DrEnableCrrCommand.DrEnableCrrCommandResult))]
[JsonSerializable(typeof(DrCrossRegionRestoreCommand.DrCrossRegionRestoreCommandResult))]
[JsonSerializable(typeof(DrValidateReadinessCommand.DrValidateReadinessCommandResult))]
// Cost
[JsonSerializable(typeof(CostEstimateCommand.CostEstimateCommandResult))]
// Diagnostics
[JsonSerializable(typeof(DiagnosticsDiagnoseCommand.DiagnosticsDiagnoseCommandResult))]
[JsonSerializable(typeof(DiagnosticsValidateCommand.DiagnosticsValidateCommandResult))]
[JsonSerializable(typeof(DiagnosticsHealthCheckCommand.DiagnosticsHealthCheckCommandResult))]
// Bulk
[JsonSerializable(typeof(BulkEnableCommand.BulkEnableCommandResult))]
[JsonSerializable(typeof(BulkTriggerCommand.BulkTriggerCommandResult))]
[JsonSerializable(typeof(BulkUpdatePolicyCommand.BulkUpdatePolicyCommandResult))]
// IaC
[JsonSerializable(typeof(IacGenerateCommand.IacGenerateCommandResult))]
// Workflow
[JsonSerializable(typeof(WorkflowSetupVmCommand.WorkflowSetupVmCommandResult))]
[JsonSerializable(typeof(WorkflowSetupSqlHanaCommand.WorkflowSetupSqlHanaCommandResult))]
[JsonSerializable(typeof(WorkflowSetupAksCommand.WorkflowSetupAksCommandResult))]
[JsonSerializable(typeof(WorkflowSetupDatasourceCommand.WorkflowSetupDatasourceCommandResult))]
[JsonSerializable(typeof(WorkflowSecureVaultCommand.WorkflowSecureVaultCommandResult))]
[JsonSerializable(typeof(WorkflowSetupDrCommand.WorkflowSetupDrCommandResult))]
[JsonSerializable(typeof(WorkflowComplianceCommand.WorkflowComplianceCommandResult))]
[JsonSerializable(typeof(WorkflowMigrateCommand.WorkflowMigrateCommandResult))]
[JsonSerializable(typeof(WorkflowRansomwareCommand.WorkflowRansomwareCommandResult))]
// Model types
[JsonSerializable(typeof(BackupVaultInfo))]
[JsonSerializable(typeof(ProtectedItemInfo))]
[JsonSerializable(typeof(BackupPolicyInfo))]
[JsonSerializable(typeof(BackupJobInfo))]
[JsonSerializable(typeof(RecoveryPointInfo))]
[JsonSerializable(typeof(VaultCreateResult))]
[JsonSerializable(typeof(ProtectResult))]
[JsonSerializable(typeof(BackupTriggerResult))]
[JsonSerializable(typeof(RestoreTriggerResult))]
[JsonSerializable(typeof(OperationResult))]
[JsonSerializable(typeof(BackupStatusResult))]
[JsonSerializable(typeof(CostEstimateResult))]
[JsonSerializable(typeof(HealthCheckResult))]
[JsonSerializable(typeof(HealthCheckItemDetail))]
[JsonSerializable(typeof(UnprotectedResourceInfo))]
[JsonSerializable(typeof(DrValidationResult))]
[JsonSerializable(typeof(WorkflowResult))]
[JsonSerializable(typeof(WorkflowStep))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
internal sealed partial class AzureBackupJsonContext : JsonSerializerContext
{
}
