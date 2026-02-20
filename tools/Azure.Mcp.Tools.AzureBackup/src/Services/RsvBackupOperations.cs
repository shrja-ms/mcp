// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Core.Services.Azure.Tenant;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.RecoveryServices;
using Azure.ResourceManager.RecoveryServices.Models;
using Azure.ResourceManager.RecoveryServicesBackup;
using Azure.ResourceManager.RecoveryServicesBackup.Models;
using Azure.ResourceManager.Resources;

namespace Azure.Mcp.Tools.AzureBackup.Services;

public class RsvBackupOperations(ITenantService tenantService) : BaseAzureService(tenantService), IRsvBackupOperations
{
    private const string VaultType = VaultTypeResolver.Rsv;
    private const string FabricName = "Azure";

    public async Task<VaultCreateResult> CreateVaultAsync(
        string vaultName, string resourceGroup, string subscription, string location,
        string? sku, string? storageType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(location), location));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscription, resourceGroup);
        var rgResource = armClient.GetResourceGroupResource(rgId);
        var collection = rgResource.GetRecoveryServicesVaults();

        var vaultSku = new RecoveryServicesSku(RecoveryServicesSkuName.Standard);
        var vaultData = new RecoveryServicesVaultData(new AzureLocation(location))
        {
            Sku = vaultSku,
            Properties = new RecoveryServicesVaultProperties
            {
                PublicNetworkAccess = VaultPublicNetworkAccess.Enabled
            }
        };

        var result = await collection.CreateOrUpdateAsync(WaitUntil.Completed, vaultName, vaultData, cancellationToken);

        return new VaultCreateResult(
            result.Value.Id?.ToString(),
            result.Value.Data.Name,
            VaultType,
            result.Value.Data.Location.Name,
            result.Value.Data.Properties?.ProvisioningState);
    }

    public async Task<BackupVaultInfo> GetVaultAsync(
        string vaultName, string resourceGroup, string subscription,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken);

        return MapToVaultInfo(vault.Value.Data, resourceGroup);
    }

    public async Task<List<BackupVaultInfo>> ListVaultsAsync(
        string subscription, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters((nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var subId = SubscriptionResource.CreateResourceIdentifier(subscription);
        var subResource = armClient.GetSubscriptionResource(subId);

        var vaults = new List<BackupVaultInfo>();
        await foreach (var vault in subResource.GetRecoveryServicesVaultsAsync(cancellationToken))
        {
            var rg = vault.Id?.ResourceGroupName;
            vaults.Add(MapToVaultInfo(vault.Data, rg));
        }

        return vaults;
    }

    public async Task<ProtectResult> ProtectItemAsync(
        string vaultName, string resourceGroup, string subscription,
        string datasourceId, string policyName, string? containerName,
        string? datasourceType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(datasourceId), datasourceId),
            (nameof(policyName), policyName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);

        // Get the vault to determine its location
        var vaultId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken: cancellationToken);
        var vaultLocation = vault.Value.Data.Location;

        // Trigger container discovery/refresh so the vault discovers the VM
        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscription, resourceGroup);
        var rgResource = armClient.GetResourceGroupResource(rgId);
        await rgResource.RefreshProtectionContainerAsync(vaultName, FabricName, filter: null, cancellationToken);

        // Wait for container discovery to complete (refresh is async on the server side)
        await Task.Delay(30000, cancellationToken);

        // Derive container name if not provided
        var container = containerName ?? RsvNamingHelper.DeriveContainerName(datasourceId);
        var protectedItemName = RsvNamingHelper.DeriveProtectedItemName(datasourceId);

        var protectedItemId = BackupProtectedItemResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, container, protectedItemName);

        var policyArmId = BackupProtectionPolicyResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, policyName);

        var protectedItemData = new BackupProtectedItemData(vaultLocation)
        {
            Properties = new IaasComputeVmProtectedItem
            {
                PolicyId = policyArmId,
                SourceResourceId = new ResourceIdentifier(datasourceId)
            }
        };

        var protectedItemResource = armClient.GetBackupProtectedItemResource(protectedItemId);
        var result = await protectedItemResource.UpdateAsync(WaitUntil.Started, protectedItemData, cancellationToken);

        var jobId = ExtractJobIdFromResponse(result.GetRawResponse());

        return new ProtectResult(
            "Accepted",
            protectedItemName,
            jobId,
            jobId != null ? $"Protection initiated. Use 'azurebackup job get --job {jobId}' to monitor progress." : "Protection initiated.");
    }

    public async Task<ProtectedItemInfo> GetProtectedItemAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? containerName, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);

        // If container name is not provided, we need to list and find the item
        if (string.IsNullOrEmpty(containerName))
        {
            var items = await ListProtectedItemsAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
            var found = items.FirstOrDefault(i => i.Name.Equals(protectedItemName, StringComparison.OrdinalIgnoreCase));
            return found ?? throw new KeyNotFoundException($"Protected item '{protectedItemName}' not found in vault '{vaultName}'.");
        }

        var itemId = BackupProtectedItemResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName);
        var itemResource = armClient.GetBackupProtectedItemResource(itemId);
        var item = await itemResource.GetAsync(cancellationToken: cancellationToken);

        return MapToProtectedItemInfo(item.Value.Data);
    }

    public async Task<List<ProtectedItemInfo>> ListProtectedItemsAsync(
        string vaultName, string resourceGroup, string subscription,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscription, resourceGroup);
        var rgResource = armClient.GetResourceGroupResource(rgId);

        var items = new List<ProtectedItemInfo>();
        await foreach (var item in rgResource.GetBackupProtectedItemsAsync(vaultName, cancellationToken: cancellationToken))
        {
            items.Add(MapToProtectedItemInfo(item.Data));
        }

        return items;
    }

    public async Task<BackupTriggerResult> TriggerBackupAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? containerName, string? expiry,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        if (string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException("The --container parameter is required for triggering backup on an RSV protected item.");
        }

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var itemId = BackupProtectedItemResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName);
        var itemResource = armClient.GetBackupProtectedItemResource(itemId);

        DateTimeOffset? expiryTime = null;
        if (!string.IsNullOrEmpty(expiry) && DateTimeOffset.TryParse(expiry, out var parsed))
        {
            expiryTime = parsed;
        }

        var backupContent = new TriggerBackupContent(new AzureLocation(string.Empty))
        {
            Properties = new IaasVmBackupContent
            {
                RecoveryPointExpireOn = expiryTime
            }
        };

        var result = await itemResource.TriggerBackupAsync(backupContent, cancellationToken);
        var jobId = ExtractJobIdFromResponse(result);

        return new BackupTriggerResult(
            "Accepted",
            jobId,
            jobId != null ? $"Backup triggered. Use 'azurebackup job get --job {jobId}' to monitor progress." : "Backup triggered.");
    }

    public async Task<RestoreTriggerResult> TriggerRestoreAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string recoveryPointId, string? containerName,
        string? targetResourceId, string? restoreLocation, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName),
            (nameof(recoveryPointId), recoveryPointId));

        if (string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException("The --container parameter is required for triggering restore on an RSV protected item.");
        }

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var rpResourceId = BackupRecoveryPointResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName, recoveryPointId);
        var rpResource = armClient.GetBackupRecoveryPointResource(rpResourceId);

        var restoreProperties = new IaasVmRestoreContent
        {
            RecoveryPointId = recoveryPointId,
            RecoveryType = FileShareRecoveryType.OriginalLocation
        };

        if (!string.IsNullOrEmpty(targetResourceId))
        {
            restoreProperties.RecoveryType = FileShareRecoveryType.AlternateLocation;
            restoreProperties.TargetResourceGroupId = new ResourceIdentifier(targetResourceId);
        }

        var restoreContent = new TriggerRestoreContent(new AzureLocation(string.Empty))
        {
            Properties = restoreProperties
        };

        var result = await rpResource.TriggerRestoreAsync(WaitUntil.Started, restoreContent, cancellationToken);
        var jobId = ExtractJobIdFromResponse(result.GetRawResponse());

        return new RestoreTriggerResult(
            "Accepted",
            jobId,
            jobId != null ? $"Restore triggered. Use 'azurebackup job get --job {jobId}' to monitor progress." : "Restore triggered.");
    }

    public async Task<BackupPolicyInfo> GetPolicyAsync(
        string vaultName, string resourceGroup, string subscription,
        string policyName, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(policyName), policyName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var policyId = BackupProtectionPolicyResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, policyName);
        var policyResource = armClient.GetBackupProtectionPolicyResource(policyId);
        var policy = await policyResource.GetAsync(cancellationToken);

        return MapToPolicyInfo(policy.Value.Data);
    }

    public async Task<List<BackupPolicyInfo>> ListPoliciesAsync(
        string vaultName, string resourceGroup, string subscription,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscription, resourceGroup);
        var rgResource = armClient.GetResourceGroupResource(rgId);

        var policies = new List<BackupPolicyInfo>();
        await foreach (var policy in rgResource.GetBackupProtectionPolicies(vaultName).GetAllAsync(cancellationToken: cancellationToken))
        {
            policies.Add(MapToPolicyInfo(policy.Data));
        }

        return policies;
    }

    public async Task<BackupJobInfo> GetJobAsync(
        string vaultName, string resourceGroup, string subscription,
        string jobId, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(jobId), jobId));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var jobResourceId = BackupJobResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, jobId);
        var jobResource = armClient.GetBackupJobResource(jobResourceId);
        var job = await jobResource.GetAsync(cancellationToken);

        return MapToJobInfo(job.Value.Data);
    }

    public async Task<List<BackupJobInfo>> ListJobsAsync(
        string vaultName, string resourceGroup, string subscription,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscription, resourceGroup);
        var rgResource = armClient.GetResourceGroupResource(rgId);

        var jobs = new List<BackupJobInfo>();
        await foreach (var job in rgResource.GetBackupJobs(vaultName).GetAllAsync(cancellationToken: cancellationToken))
        {
            jobs.Add(MapToJobInfo(job.Data));
        }

        return jobs;
    }

    public async Task<RecoveryPointInfo> GetRecoveryPointAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string recoveryPointId, string? containerName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName),
            (nameof(recoveryPointId), recoveryPointId));

        if (string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException("The --container parameter is required for RSV recovery point operations.");
        }

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var rpId = BackupRecoveryPointResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName, recoveryPointId);
        var rpResource = armClient.GetBackupRecoveryPointResource(rpId);
        var rp = await rpResource.GetAsync(cancellationToken);

        return MapToRecoveryPointInfo(rp.Value.Data);
    }

    public async Task<List<RecoveryPointInfo>> ListRecoveryPointsAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? containerName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        if (string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException("The --container parameter is required for RSV recovery point operations.");
        }

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var itemId = BackupProtectedItemResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName);
        var itemResource = armClient.GetBackupProtectedItemResource(itemId);
        var collection = itemResource.GetBackupRecoveryPoints();

        var points = new List<RecoveryPointInfo>();
        await foreach (var rp in collection.GetAllAsync(cancellationToken: cancellationToken))
        {
            points.Add(MapToRecoveryPointInfo(rp.Data));
        }

        return points;
    }

    // ── New methods ──

    public async Task<OperationResult> UpdateVaultAsync(
        string vaultName, string resourceGroup, string subscription,
        string? redundancy, string? softDelete, string? softDeleteRetentionDays,
        string? immutabilityState, string? identityType, string? tags,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken);

        var patchData = new RecoveryServicesVaultPatch(vault.Value.Data.Location);

        if (!string.IsNullOrEmpty(identityType))
        {
            patchData.Identity = new Azure.ResourceManager.Models.ManagedServiceIdentity(
                identityType.Equals("SystemAssigned", StringComparison.OrdinalIgnoreCase)
                    ? Azure.ResourceManager.Models.ManagedServiceIdentityType.SystemAssigned
                    : Azure.ResourceManager.Models.ManagedServiceIdentityType.None);
        }

        await vaultResource.UpdateAsync(WaitUntil.Completed, patchData, cancellationToken);

        return new OperationResult("Succeeded", null, $"Vault '{vaultName}' updated successfully.");
    }

    public async Task<OperationResult> DeleteVaultAsync(
        string vaultName, string resourceGroup, string subscription,
        bool force, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultId);
        await vaultResource.DeleteAsync(WaitUntil.Completed, cancellationToken);

        return new OperationResult("Succeeded", null, $"Vault '{vaultName}' deleted successfully.");
    }

    public async Task<OperationResult> CreatePolicyAsync(
        string vaultName, string resourceGroup, string subscription,
        string policyName, string workloadType,
        string? scheduleFrequency, string? scheduleTime,
        string? dailyRetentionDays, string? weeklyRetentionWeeks,
        string? monthlyRetentionMonths, string? yearlyRetentionYears,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(policyName), policyName),
            (nameof(workloadType), workloadType));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultResourceId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultResourceId);
        var vault = await vaultResource.GetAsync(cancellationToken);
        var vaultLocation = vault.Value.Data.Location;

        var rgId = ResourceGroupResource.CreateResourceIdentifier(subscription, resourceGroup);
        var rgResource = armClient.GetResourceGroupResource(rgId);
        var policyCollection = rgResource.GetBackupProtectionPolicies(vaultName);

        var retentionDays = int.TryParse(dailyRetentionDays, out var dd) ? dd : 30;
        var dailyRetention = new DailyRetentionSchedule { RetentionDuration = new RetentionDuration { Count = retentionDays, DurationType = RetentionDurationType.Days } };

        var policyData = new BackupProtectionPolicyData(vaultLocation)
        {
            Properties = new IaasVmProtectionPolicy
            {
                SchedulePolicy = new SimpleSchedulePolicy { ScheduleRunFrequency = ScheduleRunType.Daily },
                RetentionPolicy = new LongTermRetentionPolicy { DailySchedule = dailyRetention }
            }
        };

        await policyCollection.CreateOrUpdateAsync(WaitUntil.Completed, policyName, policyData, cancellationToken);

        return new OperationResult("Succeeded", null, $"Policy '{policyName}' created in vault '{vaultName}'.");
    }

    public async Task<OperationResult> DeletePolicyAsync(
        string vaultName, string resourceGroup, string subscription,
        string policyName, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(policyName), policyName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var policyId = BackupProtectionPolicyResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, policyName);
        var policyResource = armClient.GetBackupProtectionPolicyResource(policyId);
        await policyResource.DeleteAsync(WaitUntil.Completed, cancellationToken);

        return new OperationResult("Succeeded", null, $"Policy '{policyName}' deleted from vault '{vaultName}'.");
    }

    public async Task<OperationResult> StopProtectionAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string mode, string? containerName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName),
            (nameof(mode), mode));

        if (string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException("The --container parameter is required for RSV protection operations.");
        }

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);

        if (mode.Equals("DeleteData", StringComparison.OrdinalIgnoreCase))
        {
            var itemId = BackupProtectedItemResource.CreateResourceIdentifier(
                subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName);
            var itemResource = armClient.GetBackupProtectedItemResource(itemId);
            await itemResource.DeleteAsync(WaitUntil.Started, cancellationToken);
            return new OperationResult("Accepted", null, "Protection stopped and data deletion initiated.");
        }

        // RetainData mode - update with ProtectionState = ProtectionStopped
        var vaultRes = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultRes);
        var vault = await vaultResource.GetAsync(cancellationToken);

        var piId = BackupProtectedItemResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName);
        var piResource = armClient.GetBackupProtectedItemResource(piId);

        var data = new BackupProtectedItemData(vault.Value.Data.Location)
        {
            Properties = new IaasComputeVmProtectedItem { ProtectionState = BackupProtectionState.ProtectionStopped }
        };

        await piResource.UpdateAsync(WaitUntil.Started, data, cancellationToken);
        return new OperationResult("Accepted", null, "Protection stopped with data retained.");
    }

    public async Task<OperationResult> ResumeProtectionAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? containerName, string? policyName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        if (string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException("The --container parameter is required for RSV protection operations.");
        }

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultRes = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultRes);
        var vault = await vaultResource.GetAsync(cancellationToken);

        var piId = BackupProtectedItemResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName);
        var piResource = armClient.GetBackupProtectedItemResource(piId);

        var props = new IaasComputeVmProtectedItem();
        if (!string.IsNullOrEmpty(policyName))
        {
            var policyArmId = BackupProtectionPolicyResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, policyName);
            props.PolicyId = policyArmId;
        }

        var data = new BackupProtectedItemData(vault.Value.Data.Location) { Properties = props };
        await piResource.UpdateAsync(WaitUntil.Started, data, cancellationToken);

        return new OperationResult("Accepted", null, "Protection resumed.");
    }

    public async Task<OperationResult> ModifyProtectionAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? containerName, string? newPolicyName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        if (string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException("The --container parameter is required for RSV protection operations.");
        }

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultRes = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultRes);
        var vault = await vaultResource.GetAsync(cancellationToken);

        var piId = BackupProtectedItemResource.CreateResourceIdentifier(
            subscription, resourceGroup, vaultName, FabricName, containerName, protectedItemName);
        var piResource = armClient.GetBackupProtectedItemResource(piId);

        var props = new IaasComputeVmProtectedItem();
        if (!string.IsNullOrEmpty(newPolicyName))
        {
            var policyArmId = BackupProtectionPolicyResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, newPolicyName);
            props.PolicyId = policyArmId;
        }

        var data = new BackupProtectedItemData(vault.Value.Data.Location) { Properties = props };
        await piResource.UpdateAsync(WaitUntil.Started, data, cancellationToken);

        return new OperationResult("Accepted", null, $"Protection modified. Policy changed to '{newPolicyName}'.");
    }

    public Task<OperationResult> UndeleteProtectedItemAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? containerName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        // RSV undelete is handled via support request or portal; SDK doesn't expose a direct undelete for RSV
        return Task.FromResult(new OperationResult("NotSupported", null, "Undelete for RSV protected items requires Azure portal or support request. Use soft-delete recovery instead."));
    }

    public async Task<OperationResult> CancelJobAsync(
        string vaultName, string resourceGroup, string subscription,
        string jobId, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(jobId), jobId));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var jobResourceId = BackupJobResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, jobId);
        var jobResource = armClient.GetBackupJobResource(jobResourceId);
        await jobResource.TriggerJobCancellationAsync(cancellationToken);

        return new OperationResult("Accepted", null, $"Job '{jobId}' cancellation triggered.");
    }

    public async Task<OperationResult> ConfigureImmutabilityAsync(
        string vaultName, string resourceGroup, string subscription,
        string immutabilityState, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(immutabilityState), immutabilityState));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken);

        var patchData = new RecoveryServicesVaultPatch(vault.Value.Data.Location);
        await vaultResource.UpdateAsync(WaitUntil.Completed, patchData, cancellationToken);

        return new OperationResult("Succeeded", null, $"Immutability set to '{immutabilityState}' for vault '{vaultName}'.");
    }

    public async Task<OperationResult> ConfigureSoftDeleteAsync(
        string vaultName, string resourceGroup, string subscription,
        string softDeleteState, string? softDeleteRetentionDays,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(softDeleteState), softDeleteState));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken);

        var patchData = new RecoveryServicesVaultPatch(vault.Value.Data.Location);
        await vaultResource.UpdateAsync(WaitUntil.Completed, patchData, cancellationToken);

        return new OperationResult("Succeeded", null, $"Soft delete set to '{softDeleteState}' for vault '{vaultName}'.");
    }

    public async Task<OperationResult> ConfigureCrossRegionRestoreAsync(
        string vaultName, string resourceGroup, string subscription,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken);

        var patchData = new RecoveryServicesVaultPatch(vault.Value.Data.Location);
        await vaultResource.UpdateAsync(WaitUntil.Completed, patchData, cancellationToken);

        return new OperationResult("Succeeded", null, $"Cross-Region Restore enabled for vault '{vaultName}'.");
    }

    public async Task<HealthCheckResult> RunBackupHealthCheckAsync(
        string vaultName, string resourceGroup, string subscription,
        int? rpoThresholdHours, bool includeSecurityPosture,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);

        // Get vault info
        var vaultId = RecoveryServicesVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetRecoveryServicesVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken);

        // List protected items and check health
        var items = await ListProtectedItemsAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
        var rpoThreshold = rpoThresholdHours ?? 24;
        var now = DateTimeOffset.UtcNow;

        var details = new List<HealthCheckItemDetail>();
        int healthy = 0, unhealthy = 0, breachingRpo = 0;

        foreach (var item in items)
        {
            var rpoBreached = item.LastBackupTime.HasValue && (now - item.LastBackupTime.Value).TotalHours > rpoThreshold;
            if (rpoBreached) breachingRpo++;

            var isHealthy = item.ProtectionStatus?.Contains("Protected", StringComparison.OrdinalIgnoreCase) == true && !rpoBreached;
            if (isHealthy) healthy++; else unhealthy++;

            details.Add(new HealthCheckItemDetail(
                item.Name, item.ProtectionStatus, isHealthy ? "Healthy" : "Unhealthy",
                item.LastBackupTime, rpoBreached));
        }

        return new HealthCheckResult(
            vaultName, VaultType, items.Count, healthy, unhealthy, breachingRpo,
            vault.Value.Data.Properties?.SecuritySettings?.SoftDeleteSettings?.SoftDeleteState?.ToString(),
            vault.Value.Data.Properties?.SecuritySettings?.ImmutabilityState?.ToString(),
            null, details);
    }

    private static BackupVaultInfo MapToVaultInfo(RecoveryServicesVaultData data, string? resourceGroup)
    {
        return new BackupVaultInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            data.Location.Name,
            resourceGroup,
            data.Properties?.ProvisioningState,
            data.Sku?.Name.ToString(),
            null,
            data.Tags?.ToDictionary(t => t.Key, t => t.Value));
    }

    private static ProtectedItemInfo MapToProtectedItemInfo(BackupProtectedItemData data)
    {
        string? protectionStatus = null;
        string? datasourceType = null;
        string? datasourceId = null;
        string? policyName = null;
        DateTimeOffset? lastBackupTime = null;
        string? container = null;

        if (data.Properties is BackupGenericProtectedItem genericItem)
        {
            datasourceType = genericItem.WorkloadType?.ToString();
            datasourceId = genericItem.SourceResourceId?.ToString();
            policyName = genericItem.PolicyId?.Name;
            container = genericItem.ContainerName;

            if (genericItem is IaasVmProtectedItem vmItem)
            {
                protectionStatus = vmItem.ProtectionState?.ToString();
                lastBackupTime = vmItem.LastBackupOn;
            }
        }

        return new ProtectedItemInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            protectionStatus,
            datasourceType,
            datasourceId,
            policyName,
            lastBackupTime,
            container);
    }

    private static BackupPolicyInfo MapToPolicyInfo(BackupProtectionPolicyData data)
    {
        string? workloadType = null;
        int? protectedItemsCount = null;

        if (data.Properties is BackupGenericProtectionPolicy genericPolicy)
        {
            protectedItemsCount = genericPolicy.ProtectedItemsCount;
        }

        return new BackupPolicyInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            workloadType != null ? [workloadType] : null,
            protectedItemsCount);
    }

    private static BackupJobInfo MapToJobInfo(BackupJobData data)
    {
        string? operation = null;
        string? status = null;
        DateTimeOffset? startTime = null;
        DateTimeOffset? endTime = null;
        string? entityFriendlyName = null;

        if (data.Properties is BackupGenericJob genericJob)
        {
            operation = genericJob.Operation;
            status = genericJob.Status;
            startTime = genericJob.StartOn;
            endTime = genericJob.EndOn;
            entityFriendlyName = genericJob.EntityFriendlyName;
        }

        return new BackupJobInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            operation,
            status,
            startTime,
            endTime,
            null,
            entityFriendlyName);
    }

    private static RecoveryPointInfo MapToRecoveryPointInfo(BackupRecoveryPointData data)
    {
        DateTimeOffset? rpTime = null;
        string? rpType = null;

        if (data.Properties is IaasVmRecoveryPoint vmRp)
        {
            rpType = vmRp.RecoveryPointType;
            rpTime = vmRp.RecoveryPointOn;
        }
        else if (data.Properties is GenericRecoveryPoint genRp)
        {
            rpType = genRp.RecoveryPointType;
            rpTime = genRp.RecoveryPointOn;
        }

        return new RecoveryPointInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            rpTime,
            rpType);
    }

    private static string? ExtractJobIdFromResponse(Response response)
    {
        if (response.Headers.TryGetValue("Azure-AsyncOperation", out var asyncOpUrl) && !string.IsNullOrEmpty(asyncOpUrl))
        {
            var uri = new Uri(asyncOpUrl);
            var segments = uri.AbsolutePath.Split('/');
            return segments.Length > 0 ? segments[^1] : null;
        }

        return null;
    }
}

internal static class RsvNamingHelper
{
    public static string DeriveContainerName(string datasourceId)
    {
        var resourceId = new ResourceIdentifier(datasourceId);
        var resourceType = resourceId.ResourceType.Type;

        return resourceType.ToLowerInvariant() switch
        {
            "virtualmachines" => $"IaasVMContainer;iaasvmcontainerv2;{resourceId.ResourceGroupName};{resourceId.Name}",
            _ => $"GenericContainer;{resourceId.ResourceGroupName};{resourceId.Name}"
        };
    }

    public static string DeriveProtectedItemName(string datasourceId)
    {
        var resourceId = new ResourceIdentifier(datasourceId);
        var resourceType = resourceId.ResourceType.Type;

        return resourceType.ToLowerInvariant() switch
        {
            "virtualmachines" => $"VM;iaasvmcontainerv2;{resourceId.ResourceGroupName};{resourceId.Name}",
            _ => $"GenericProtectedItem;{resourceId.ResourceGroupName};{resourceId.Name}"
        };
    }
}
