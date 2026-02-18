// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.AzureBackup.Commands.Backup;
using Azure.Mcp.Tools.AzureBackup.Commands.Job;
using Azure.Mcp.Tools.AzureBackup.Commands.Policy;
using Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Commands.RecoveryPoint;
using Azure.Mcp.Tools.AzureBackup.Commands.Restore;
using Azure.Mcp.Tools.AzureBackup.Commands.Vault;
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

        services.AddSingleton<VaultListCommand>();
        services.AddSingleton<VaultGetCommand>();
        services.AddSingleton<VaultCreateCommand>();
        services.AddSingleton<ProtectedItemListCommand>();
        services.AddSingleton<ProtectedItemGetCommand>();
        services.AddSingleton<ProtectedItemProtectCommand>();
        services.AddSingleton<BackupTriggerCommand>();
        services.AddSingleton<RestoreTriggerCommand>();
        services.AddSingleton<PolicyListCommand>();
        services.AddSingleton<PolicyGetCommand>();
        services.AddSingleton<JobListCommand>();
        services.AddSingleton<JobGetCommand>();
        services.AddSingleton<RecoveryPointListCommand>();
        services.AddSingleton<RecoveryPointGetCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var azureBackup = new CommandGroup(Name,
            """
            Azure Backup operations – Unified commands to manage backup across Recovery Services vaults (RSV)
            and Backup vaults (DPP/Data Protection). Supports vault management, protected item operations,
            on-demand backup and restore, policy management, job monitoring, and recovery point browsing.
            Use --vault-type to specify vault type or let the system auto-detect.
            """,
            Title);

        // Vault subgroup
        var vault = new CommandGroup("vault", "Backup vault operations - Create, get, and list Recovery Services vaults and Backup vaults.");
        azureBackup.AddSubGroup(vault);

        var vaultList = serviceProvider.GetRequiredService<VaultListCommand>();
        vault.AddCommand(vaultList.Name, vaultList);
        var vaultGet = serviceProvider.GetRequiredService<VaultGetCommand>();
        vault.AddCommand(vaultGet.Name, vaultGet);
        var vaultCreate = serviceProvider.GetRequiredService<VaultCreateCommand>();
        vault.AddCommand(vaultCreate.Name, vaultCreate);

        // Protected item subgroup
        var protectedItem = new CommandGroup("protecteditem", "Protected item operations - Protect resources, get and list protected items/backup instances.");
        azureBackup.AddSubGroup(protectedItem);

        var protectedItemList = serviceProvider.GetRequiredService<ProtectedItemListCommand>();
        protectedItem.AddCommand(protectedItemList.Name, protectedItemList);
        var protectedItemGet = serviceProvider.GetRequiredService<ProtectedItemGetCommand>();
        protectedItem.AddCommand(protectedItemGet.Name, protectedItemGet);
        var protectedItemProtect = serviceProvider.GetRequiredService<ProtectedItemProtectCommand>();
        protectedItem.AddCommand(protectedItemProtect.Name, protectedItemProtect);

        // Backup subgroup
        var backup = new CommandGroup("backup", "Backup operations - Trigger on-demand backups for protected items.");
        azureBackup.AddSubGroup(backup);

        var backupTrigger = serviceProvider.GetRequiredService<BackupTriggerCommand>();
        backup.AddCommand(backupTrigger.Name, backupTrigger);

        // Restore subgroup
        var restore = new CommandGroup("restore", "Restore operations - Trigger restore from recovery points.");
        azureBackup.AddSubGroup(restore);

        var restoreTrigger = serviceProvider.GetRequiredService<RestoreTriggerCommand>();
        restore.AddCommand(restoreTrigger.Name, restoreTrigger);

        // Policy subgroup
        var policy = new CommandGroup("policy", "Backup policy operations - Get and list backup policies.");
        azureBackup.AddSubGroup(policy);

        var policyList = serviceProvider.GetRequiredService<PolicyListCommand>();
        policy.AddCommand(policyList.Name, policyList);
        var policyGet = serviceProvider.GetRequiredService<PolicyGetCommand>();
        policy.AddCommand(policyGet.Name, policyGet);

        // Job subgroup
        var job = new CommandGroup("job", "Backup job operations - Get and list backup jobs to monitor operation progress.");
        azureBackup.AddSubGroup(job);

        var jobList = serviceProvider.GetRequiredService<JobListCommand>();
        job.AddCommand(jobList.Name, jobList);
        var jobGet = serviceProvider.GetRequiredService<JobGetCommand>();
        job.AddCommand(jobGet.Name, jobGet);

        // Recovery point subgroup
        var recoveryPoint = new CommandGroup("recoverypoint", "Recovery point operations - Get and list available recovery points for restore.");
        azureBackup.AddSubGroup(recoveryPoint);

        var rpList = serviceProvider.GetRequiredService<RecoveryPointListCommand>();
        recoveryPoint.AddCommand(rpList.Name, rpList);
        var rpGet = serviceProvider.GetRequiredService<RecoveryPointGetCommand>();
        recoveryPoint.AddCommand(rpGet.Name, rpGet);

        return azureBackup;
    }
}
