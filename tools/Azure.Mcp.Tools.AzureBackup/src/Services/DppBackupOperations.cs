// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Core.Services.Azure.Tenant;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.DataProtectionBackup;
using Azure.ResourceManager.DataProtectionBackup.Models;
using Azure.ResourceManager.Resources;

namespace Azure.Mcp.Tools.AzureBackup.Services;

public class DppBackupOperations(ITenantService tenantService) : BaseAzureService(tenantService), IDppBackupOperations
{
    private const string VaultType = VaultTypeResolver.Dpp;

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
        var collection = rgResource.GetDataProtectionBackupVaults();

        var storageSettings = new List<DataProtectionBackupStorageSetting>
        {
            new()
            {
                DataStoreType = StorageSettingStoreType.VaultStore,
                StorageSettingType = storageType?.ToLowerInvariant() switch
                {
                    "locallyredundant" => StorageSettingType.LocallyRedundant,
                    "zoneredundant" => StorageSettingType.ZoneRedundant,
                    _ => StorageSettingType.GeoRedundant
                }
            }
        };

        var vaultData = new DataProtectionBackupVaultData(new AzureLocation(location), new DataProtectionBackupVaultProperties(storageSettings));

        var result = await collection.CreateOrUpdateAsync(WaitUntil.Completed, vaultName, vaultData, cancellationToken);

        return new VaultCreateResult(
            result.Value.Id?.ToString(),
            result.Value.Data.Name,
            VaultType,
            result.Value.Data.Location.Name,
            result.Value.Data.Properties?.ProvisioningState?.ToString());
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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
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
        await foreach (var vault in subResource.GetDataProtectionBackupVaultsAsync(cancellationToken))
        {
            var rg = vault.Id?.ResourceGroupName;
            vaults.Add(MapToVaultInfo(vault.Data, rg));
        }

        return vaults;
    }

    public async Task<ProtectResult> ProtectItemAsync(
        string vaultName, string resourceGroup, string subscription,
        string datasourceId, string policyName, string? datasourceType,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(datasourceId), datasourceId),
            (nameof(policyName), policyName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        var collection = vaultResource.GetDataProtectionBackupInstances();

        var policyId = DataProtectionBackupPolicyResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, policyName);
        var datasourceResourceId = new ResourceIdentifier(datasourceId);
        var instanceName = $"{datasourceResourceId.Name}-{datasourceResourceId.Name}-{Guid.NewGuid().ToString("N")[..12]}";

        var policyInfo = new BackupInstancePolicyInfo(policyId);
        var instanceProperties = new DataProtectionBackupInstanceProperties(
            new DataSourceInfo(datasourceResourceId),
            policyInfo,
            string.Empty);
        var instanceData = new DataProtectionBackupInstanceData
        {
            Properties = instanceProperties
        };

        var result = await collection.CreateOrUpdateAsync(WaitUntil.Started, instanceName, instanceData, cancellationToken);

        var jobId = ExtractJobIdFromOperation(result.GetRawResponse());

        return new ProtectResult(
            "Accepted",
            instanceName,
            jobId,
            jobId != null ? $"Protection initiated. Use 'azurebackup job get --job {jobId}' to monitor progress." : "Protection initiated.");
    }

    public async Task<ProtectedItemInfo> GetProtectedItemAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var instanceId = DataProtectionBackupInstanceResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName);
        var instanceResource = armClient.GetDataProtectionBackupInstanceResource(instanceId);
        var instance = await instanceResource.GetAsync(cancellationToken);

        return MapToProtectedItemInfo(instance.Value.Data);
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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        var collection = vaultResource.GetDataProtectionBackupInstances();

        var items = new List<ProtectedItemInfo>();
        await foreach (var instance in collection.GetAllAsync(cancellationToken))
        {
            items.Add(MapToProtectedItemInfo(instance.Data));
        }

        return items;
    }

    public async Task<BackupTriggerResult> TriggerBackupAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? expiry, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var instanceId = DataProtectionBackupInstanceResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName);
        var instanceResource = armClient.GetDataProtectionBackupInstanceResource(instanceId);

        var ruleOption = new AdhocBackupRules("BackupNow", "Default");
        var backupContent = new AdhocBackupTriggerContent(ruleOption);

        var result = await instanceResource.TriggerAdhocBackupAsync(WaitUntil.Started, backupContent, cancellationToken);
        var jobId = ExtractJobIdFromOperation(result.GetRawResponse());

        return new BackupTriggerResult(
            "Accepted",
            jobId,
            jobId != null ? $"Backup triggered. Use 'azurebackup job get --job {jobId}' to monitor progress." : "Backup triggered.");
    }

    public async Task<RestoreTriggerResult> TriggerRestoreAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string recoveryPointId,
        string? targetResourceId, string? restoreLocation, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName),
            (nameof(recoveryPointId), recoveryPointId));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var instanceId = DataProtectionBackupInstanceResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName);
        var instanceResource = armClient.GetDataProtectionBackupInstanceResource(instanceId);

        // Get the backup instance to determine datasource info
        var instance = await instanceResource.GetAsync(cancellationToken);
        var datasourceId = instance.Value.Data.Properties?.DataSourceInfo?.ResourceId;

        var targetId = !string.IsNullOrEmpty(targetResourceId) ? new ResourceIdentifier(targetResourceId) : datasourceId;
        var location = !string.IsNullOrEmpty(restoreLocation) ? new AzureLocation(restoreLocation) : instance.Value.Data.Properties?.DataSourceInfo?.ResourceLocation;

        var restoreTarget = new RestoreTargetInfo(
            new RecoverySetting(),
            new DataSourceInfo(targetId ?? datasourceId!)
            {
                ResourceLocation = location
            });

        var restoreContent = new BackupRecoveryPointBasedRestoreContent(
            restoreTarget,
            SourceDataStoreType.VaultStore,
            recoveryPointId);

        var result = await instanceResource.TriggerRestoreAsync(WaitUntil.Started, restoreContent, cancellationToken);
        var jobId = ExtractJobIdFromOperation(result.GetRawResponse());

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
        var policyId = DataProtectionBackupPolicyResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, policyName);
        var policyResource = armClient.GetDataProtectionBackupPolicyResource(policyId);
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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        var collection = vaultResource.GetDataProtectionBackupPolicies();

        var policies = new List<BackupPolicyInfo>();
        await foreach (var policy in collection.GetAllAsync(cancellationToken))
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
        var jobResourceId = DataProtectionBackupJobResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, jobId);
        var jobResource = armClient.GetDataProtectionBackupJobResource(jobResourceId);
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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        var collection = vaultResource.GetDataProtectionBackupJobs();

        var jobs = new List<BackupJobInfo>();
        await foreach (var job in collection.GetAllAsync(cancellationToken))
        {
            jobs.Add(MapToJobInfo(job.Data));
        }

        return jobs;
    }

    public async Task<RecoveryPointInfo> GetRecoveryPointAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string recoveryPointId, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName),
            (nameof(recoveryPointId), recoveryPointId));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var rpId = DataProtectionBackupRecoveryPointResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName, recoveryPointId);
        var rpResource = armClient.GetDataProtectionBackupRecoveryPointResource(rpId);
        var rp = await rpResource.GetAsync(cancellationToken);

        return MapToRecoveryPointInfo(rp.Value.Data);
    }

    public async Task<List<RecoveryPointInfo>> ListRecoveryPointsAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var instanceId = DataProtectionBackupInstanceResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName);
        var instanceResource = armClient.GetDataProtectionBackupInstanceResource(instanceId);
        var collection = instanceResource.GetDataProtectionBackupRecoveryPoints();

        var points = new List<RecoveryPointInfo>();
        await foreach (var rp in collection.GetAllAsync(cancellationToken: cancellationToken))
        {
            points.Add(MapToRecoveryPointInfo(rp.Data));
        }

        return points;
    }

    private static BackupVaultInfo MapToVaultInfo(DataProtectionBackupVaultData data, string? resourceGroup)
    {
        return new BackupVaultInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            data.Location.Name,
            resourceGroup,
            data.Properties?.ProvisioningState?.ToString(),
            null,
            data.Properties?.StorageSettings?.FirstOrDefault()?.StorageSettingType?.ToString(),
            data.Tags?.ToDictionary(t => t.Key, t => t.Value));
    }

    private static ProtectedItemInfo MapToProtectedItemInfo(DataProtectionBackupInstanceData data)
    {
        return new ProtectedItemInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            data.Properties?.ProtectionStatus?.Status?.ToString(),
            data.Properties?.DataSourceInfo?.DataSourceType,
            data.Properties?.DataSourceInfo?.ResourceId?.ToString(),
            data.Properties?.PolicyInfo?.PolicyId?.Name,
            null,
            null);
    }

    private static BackupPolicyInfo MapToPolicyInfo(DataProtectionBackupPolicyData data)
    {
        var datasourceTypes = data.Properties is DataProtectionBackupPolicyPropertiesBase props
            ? props.DataSourceTypes?.ToList() as IReadOnlyList<string>
            : null;

        return new BackupPolicyInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            datasourceTypes,
            null);
    }

    private static BackupJobInfo MapToJobInfo(DataProtectionBackupJobData data)
    {
        return new BackupJobInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            data.Properties?.OperationCategory,
            data.Properties?.Status,
            data.Properties?.StartOn,
            data.Properties?.EndOn,
            data.Properties?.DataSourceType,
            data.Properties?.DataSourceName);
    }

    private static RecoveryPointInfo MapToRecoveryPointInfo(DataProtectionBackupRecoveryPointData data)
    {
        DateTimeOffset? rpTime = null;
        string? rpType = null;

        if (data.Properties is DataProtectionBackupDiscreteRecoveryPointProperties rpProps)
        {
            rpTime = rpProps.RecoverOn;
            rpType = rpProps.RecoveryPointType;
        }

        return new RecoveryPointInfo(
            data.Id?.ToString(),
            data.Name,
            VaultType,
            rpTime,
            rpType);
    }

    private static string? ExtractJobIdFromOperation(Response response)
    {
        if (response.Headers.TryGetValue("Azure-AsyncOperation", out var asyncOpUrl) && !string.IsNullOrEmpty(asyncOpUrl))
        {
            // Format: https://.../operationResults/{jobId}?api-version=...
            var uri = new Uri(asyncOpUrl);
            var segments = uri.AbsolutePath.Split('/');
            return segments.Length > 0 ? segments[^1] : null;
        }

        return null;
    }
}
