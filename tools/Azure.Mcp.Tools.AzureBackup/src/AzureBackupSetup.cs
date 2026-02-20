// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Mcp.Core.Areas;
using Microsoft.Mcp.Core.Commands;

namespace Azure.Mcp.Tools.AzureBackup;

public class AzureBackupSetup : IAreaSetup
{
    public string Name => "azurebackup";

    public string Title => "Manage Azure Backup";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IRsvBackupOperations, RsvBackupOperations>();
        services.AddSingleton<IDppBackupOperations, DppBackupOperations>();
        services.AddSingleton<IAzureBackupService, AzureBackupService>();

        // Vault
        services.AddSingleton<VaultListCommand>();
        services.AddSingleton<VaultGetCommand>();
        services.AddSingleton<VaultCreateCommand>();
        services.AddSingleton<VaultUpdateCommand>();
        services.AddSingleton<VaultDeleteCommand>();

        // Policy
        services.AddSingleton<PolicyListCommand>();
        services.AddSingleton<PolicyGetCommand>();
        services.AddSingleton<PolicyCreateCommand>();
        services.AddSingleton<PolicyUpdateCommand>();
        services.AddSingleton<PolicyDeleteCommand>();

        // Protected item
        services.AddSingleton<ProtectedItemListCommand>();
        services.AddSingleton<ProtectedItemGetCommand>();
        services.AddSingleton<ProtectedItemProtectCommand>();
        services.AddSingleton<ProtectedItemStopCommand>();
        services.AddSingleton<ProtectedItemResumeCommand>();
        services.AddSingleton<ProtectedItemModifyCommand>();
        services.AddSingleton<ProtectedItemUndeleteCommand>();
        services.AddSingleton<ProtectedItemAutoProtectCommand>();

        // Backup
        services.AddSingleton<BackupTriggerCommand>();
        services.AddSingleton<BackupStatusCommand>();

        // Restore
        services.AddSingleton<RestoreTriggerCommand>();

        // Job
        services.AddSingleton<JobListCommand>();
        services.AddSingleton<JobGetCommand>();
        services.AddSingleton<JobCancelCommand>();

        // Recovery point
        services.AddSingleton<RecoveryPointListCommand>();
        services.AddSingleton<RecoveryPointGetCommand>();
        services.AddSingleton<RecoveryPointArchiveCommand>();

        // Security
        services.AddSingleton<SecurityRbacCommand>();
        services.AddSingleton<SecurityMuaCommand>();
        services.AddSingleton<SecurityPrivateEndpointCommand>();
        services.AddSingleton<SecurityEncryptionCommand>();

        // Monitoring
        services.AddSingleton<MonitoringConfigureCommand>();
        services.AddSingleton<MonitoringReportsCommand>();

        // Governance
        services.AddSingleton<GovernanceFindUnprotectedCommand>();
        services.AddSingleton<GovernanceApplyPolicyCommand>();
        services.AddSingleton<GovernanceImmutabilityCommand>();
        services.AddSingleton<GovernanceSoftDeleteCommand>();

        // DR
        services.AddSingleton<DrEnableCrrCommand>();
        services.AddSingleton<DrCrossRegionRestoreCommand>();
        services.AddSingleton<DrValidateReadinessCommand>();

        // Cost
        services.AddSingleton<CostEstimateCommand>();

        // Diagnostics
        services.AddSingleton<DiagnosticsDiagnoseCommand>();
        services.AddSingleton<DiagnosticsValidateCommand>();
        services.AddSingleton<DiagnosticsHealthCheckCommand>();

        // Bulk
        services.AddSingleton<BulkEnableCommand>();
        services.AddSingleton<BulkTriggerCommand>();
        services.AddSingleton<BulkUpdatePolicyCommand>();

        // IaC
        services.AddSingleton<IacGenerateCommand>();

        // Workflow
        services.AddSingleton<WorkflowSetupVmCommand>();
        services.AddSingleton<WorkflowSetupSqlHanaCommand>();
        services.AddSingleton<WorkflowSetupAksCommand>();
        services.AddSingleton<WorkflowSetupDatasourceCommand>();
        services.AddSingleton<WorkflowSecureVaultCommand>();
        services.AddSingleton<WorkflowSetupDrCommand>();
        services.AddSingleton<WorkflowComplianceCommand>();
        services.AddSingleton<WorkflowMigrateCommand>();
        services.AddSingleton<WorkflowRansomwareCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var azureBackup = new CommandGroup(Name,
            """
            Azure Backup operations – Unified commands to manage backup across Recovery Services vaults (RSV)
            and Backup vaults (DPP/Data Protection). Supports vault management, protected item operations,
            on-demand backup and restore, policy management, job monitoring, recovery point browsing,
            security configuration, monitoring, governance, DR, cost estimation, diagnostics, bulk operations,
            IaC generation, and multi-step workflows.
            Use --vault-type to specify vault type or let the system auto-detect.
            """,
            Title);

        // Vault subgroup
        var vault = new CommandGroup("vault", "Backup vault operations - Create, get, list, update, and delete vaults.");
        azureBackup.AddSubGroup(vault);
        RegisterCommand<VaultListCommand>(serviceProvider, vault);
        RegisterCommand<VaultGetCommand>(serviceProvider, vault);
        RegisterCommand<VaultCreateCommand>(serviceProvider, vault);
        RegisterCommand<VaultUpdateCommand>(serviceProvider, vault);
        RegisterCommand<VaultDeleteCommand>(serviceProvider, vault);

        // Policy subgroup
        var policy = new CommandGroup("policy", "Backup policy operations - Create, get, list, update, and delete policies.");
        azureBackup.AddSubGroup(policy);
        RegisterCommand<PolicyListCommand>(serviceProvider, policy);
        RegisterCommand<PolicyGetCommand>(serviceProvider, policy);
        RegisterCommand<PolicyCreateCommand>(serviceProvider, policy);
        RegisterCommand<PolicyUpdateCommand>(serviceProvider, policy);
        RegisterCommand<PolicyDeleteCommand>(serviceProvider, policy);

        // Protected item subgroup
        var protectedItem = new CommandGroup("protecteditem", "Protected item operations - Protect, stop, resume, modify, undelete, and auto-protect.");
        azureBackup.AddSubGroup(protectedItem);
        RegisterCommand<ProtectedItemListCommand>(serviceProvider, protectedItem);
        RegisterCommand<ProtectedItemGetCommand>(serviceProvider, protectedItem);
        RegisterCommand<ProtectedItemProtectCommand>(serviceProvider, protectedItem);
        RegisterCommand<ProtectedItemStopCommand>(serviceProvider, protectedItem);
        RegisterCommand<ProtectedItemResumeCommand>(serviceProvider, protectedItem);
        RegisterCommand<ProtectedItemModifyCommand>(serviceProvider, protectedItem);
        RegisterCommand<ProtectedItemUndeleteCommand>(serviceProvider, protectedItem);
        RegisterCommand<ProtectedItemAutoProtectCommand>(serviceProvider, protectedItem);

        // Backup subgroup
        var backup = new CommandGroup("backup", "Backup operations - Trigger on-demand backups and check backup status.");
        azureBackup.AddSubGroup(backup);
        RegisterCommand<BackupTriggerCommand>(serviceProvider, backup);
        RegisterCommand<BackupStatusCommand>(serviceProvider, backup);

        // Restore subgroup
        var restore = new CommandGroup("restore", "Restore operations - Trigger restore from recovery points.");
        azureBackup.AddSubGroup(restore);
        RegisterCommand<RestoreTriggerCommand>(serviceProvider, restore);

        // Job subgroup
        var job = new CommandGroup("job", "Backup job operations - Get, list, and cancel backup jobs.");
        azureBackup.AddSubGroup(job);
        RegisterCommand<JobListCommand>(serviceProvider, job);
        RegisterCommand<JobGetCommand>(serviceProvider, job);
        RegisterCommand<JobCancelCommand>(serviceProvider, job);

        // Recovery point subgroup
        var recoveryPoint = new CommandGroup("recoverypoint", "Recovery point operations - Get, list, and archive recovery points.");
        azureBackup.AddSubGroup(recoveryPoint);
        RegisterCommand<RecoveryPointListCommand>(serviceProvider, recoveryPoint);
        RegisterCommand<RecoveryPointGetCommand>(serviceProvider, recoveryPoint);
        RegisterCommand<RecoveryPointArchiveCommand>(serviceProvider, recoveryPoint);

        // Security subgroup
        var security = new CommandGroup("security", "Security operations - Configure RBAC, MUA, private endpoints, and encryption.");
        azureBackup.AddSubGroup(security);
        RegisterCommand<SecurityRbacCommand>(serviceProvider, security);
        RegisterCommand<SecurityMuaCommand>(serviceProvider, security);
        RegisterCommand<SecurityPrivateEndpointCommand>(serviceProvider, security);
        RegisterCommand<SecurityEncryptionCommand>(serviceProvider, security);

        // Monitoring subgroup
        var monitoring = new CommandGroup("monitoring", "Monitoring operations - Configure diagnostics and generate reports.");
        azureBackup.AddSubGroup(monitoring);
        RegisterCommand<MonitoringConfigureCommand>(serviceProvider, monitoring);
        RegisterCommand<MonitoringReportsCommand>(serviceProvider, monitoring);

        // Governance subgroup
        var governance = new CommandGroup("governance", "Governance operations - Find unprotected resources, apply policies, configure immutability and soft delete.");
        azureBackup.AddSubGroup(governance);
        RegisterCommand<GovernanceFindUnprotectedCommand>(serviceProvider, governance);
        RegisterCommand<GovernanceApplyPolicyCommand>(serviceProvider, governance);
        RegisterCommand<GovernanceImmutabilityCommand>(serviceProvider, governance);
        RegisterCommand<GovernanceSoftDeleteCommand>(serviceProvider, governance);

        // DR subgroup
        var dr = new CommandGroup("dr", "Disaster recovery operations - Enable CRR, trigger cross-region restore, validate DR readiness.");
        azureBackup.AddSubGroup(dr);
        RegisterCommand<DrEnableCrrCommand>(serviceProvider, dr);
        RegisterCommand<DrCrossRegionRestoreCommand>(serviceProvider, dr);
        RegisterCommand<DrValidateReadinessCommand>(serviceProvider, dr);

        // Cost subgroup
        var cost = new CommandGroup("cost", "Cost operations - Estimate backup costs.");
        azureBackup.AddSubGroup(cost);
        RegisterCommand<CostEstimateCommand>(serviceProvider, cost);

        // Diagnostics subgroup
        var diagnostics = new CommandGroup("diagnostics", "Diagnostics operations - Diagnose failures, validate prerequisites, and run health checks.");
        azureBackup.AddSubGroup(diagnostics);
        RegisterCommand<DiagnosticsDiagnoseCommand>(serviceProvider, diagnostics);
        RegisterCommand<DiagnosticsValidateCommand>(serviceProvider, diagnostics);
        RegisterCommand<DiagnosticsHealthCheckCommand>(serviceProvider, diagnostics);

        // Bulk subgroup
        var bulk = new CommandGroup("bulk", "Bulk operations - Enable, trigger, and update policies across multiple items.");
        azureBackup.AddSubGroup(bulk);
        RegisterCommand<BulkEnableCommand>(serviceProvider, bulk);
        RegisterCommand<BulkTriggerCommand>(serviceProvider, bulk);
        RegisterCommand<BulkUpdatePolicyCommand>(serviceProvider, bulk);

        // IaC subgroup
        var iac = new CommandGroup("iac", "Infrastructure-as-Code operations - Generate IaC templates from vault configurations.");
        azureBackup.AddSubGroup(iac);
        RegisterCommand<IacGenerateCommand>(serviceProvider, iac);

        // Workflow subgroup
        var workflow = new CommandGroup("workflow", "Multi-step workflow operations - End-to-end backup setup, security, DR, compliance, migration, and ransomware recovery.");
        azureBackup.AddSubGroup(workflow);
        RegisterCommand<WorkflowSetupVmCommand>(serviceProvider, workflow);
        RegisterCommand<WorkflowSetupSqlHanaCommand>(serviceProvider, workflow);
        RegisterCommand<WorkflowSetupAksCommand>(serviceProvider, workflow);
        RegisterCommand<WorkflowSetupDatasourceCommand>(serviceProvider, workflow);
        RegisterCommand<WorkflowSecureVaultCommand>(serviceProvider, workflow);
        RegisterCommand<WorkflowSetupDrCommand>(serviceProvider, workflow);
        RegisterCommand<WorkflowComplianceCommand>(serviceProvider, workflow);
        RegisterCommand<WorkflowMigrateCommand>(serviceProvider, workflow);
        RegisterCommand<WorkflowRansomwareCommand>(serviceProvider, workflow);

        return azureBackup;
    }

    private static void RegisterCommand<T>(IServiceProvider serviceProvider, CommandGroup group) where T : IBaseCommand
    {
        var cmd = serviceProvider.GetRequiredService<T>();
        group.AddCommand(cmd.Name, cmd);
    }
}
