// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.DataProtection.Commands.BackupInstance;
using Azure.Mcp.Tools.DataProtection.Commands.Job;
using Azure.Mcp.Tools.DataProtection.Commands.Policy;
using Azure.Mcp.Tools.DataProtection.Commands.RecoveryPoint;
using Azure.Mcp.Tools.DataProtection.Commands.Vault;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Mcp.Core.Areas;
using Microsoft.Mcp.Core.Commands;

namespace Azure.Mcp.Tools.DataProtection;

public class DataProtectionSetup : IAreaSetup
{
    public string Name => "dataprotection";

    public string Title => "Azure Data Protection";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDataProtectionService, DataProtectionService>();

        services.AddSingleton<VaultListCommand>();
        services.AddSingleton<VaultGetCommand>();
        services.AddSingleton<BackupInstanceListCommand>();
        services.AddSingleton<BackupInstanceGetCommand>();
        services.AddSingleton<PolicyListCommand>();
        services.AddSingleton<PolicyGetCommand>();
        services.AddSingleton<JobListCommand>();
        services.AddSingleton<JobGetCommand>();
        services.AddSingleton<RecoveryPointListCommand>();
        services.AddSingleton<RecoveryPointGetCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var dataprotection = new CommandGroup(Name,
            "Data Protection operations - Commands for managing Azure Backup vaults, backup instances, policies, jobs, and recovery points. Supports listing and retrieving details of data protection resources across your Azure subscriptions.",
            Title);

        // Vault sub-group
        var vault = new CommandGroup("vault", "Backup vault operations - Commands for listing and retrieving details of Azure Backup vaults.");
        dataprotection.AddSubGroup(vault);

        var vaultList = serviceProvider.GetRequiredService<VaultListCommand>();
        vault.AddCommand(vaultList.Name, vaultList);
        var vaultGet = serviceProvider.GetRequiredService<VaultGetCommand>();
        vault.AddCommand(vaultGet.Name, vaultGet);

        // Backup instance sub-group
        var backupInstance = new CommandGroup("backupinstance", "Backup instance operations - Commands for listing and retrieving details of backup instances within a vault.");
        dataprotection.AddSubGroup(backupInstance);

        var instanceList = serviceProvider.GetRequiredService<BackupInstanceListCommand>();
        backupInstance.AddCommand(instanceList.Name, instanceList);
        var instanceGet = serviceProvider.GetRequiredService<BackupInstanceGetCommand>();
        backupInstance.AddCommand(instanceGet.Name, instanceGet);

        // Policy sub-group
        var policy = new CommandGroup("policy", "Backup policy operations - Commands for listing and retrieving details of backup policies within a vault.");
        dataprotection.AddSubGroup(policy);

        var policyList = serviceProvider.GetRequiredService<PolicyListCommand>();
        policy.AddCommand(policyList.Name, policyList);
        var policyGet = serviceProvider.GetRequiredService<PolicyGetCommand>();
        policy.AddCommand(policyGet.Name, policyGet);

        // Job sub-group
        var job = new CommandGroup("job", "Backup job operations - Commands for listing and retrieving details of backup jobs within a vault.");
        dataprotection.AddSubGroup(job);

        var jobList = serviceProvider.GetRequiredService<JobListCommand>();
        job.AddCommand(jobList.Name, jobList);
        var jobGet = serviceProvider.GetRequiredService<JobGetCommand>();
        job.AddCommand(jobGet.Name, jobGet);

        // Recovery point sub-group
        var recoveryPoint = new CommandGroup("recoverypoint", "Recovery point operations - Commands for listing and retrieving details of recovery points for backup instances.");
        dataprotection.AddSubGroup(recoveryPoint);

        var rpList = serviceProvider.GetRequiredService<RecoveryPointListCommand>();
        recoveryPoint.AddCommand(rpList.Name, rpList);
        var rpGet = serviceProvider.GetRequiredService<RecoveryPointGetCommand>();
        recoveryPoint.AddCommand(rpGet.Name, rpGet);

        return dataprotection;
    }
}
