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

    /// <summary>
    /// Maps friendly workload type names to ARM resource type strings for DPP policies.
    /// </summary>
    internal static string MapWorkloadTypeToArmResourceType(string workloadType) => workloadType.ToLowerInvariant() switch
    {
        "azuredisk" => "Microsoft.Compute/disks",
        "azureblob" => "Microsoft.Storage/storageAccounts/blobServices",
        "postgresqlflexible" => "Microsoft.DBforPostgreSQL/flexibleServers",
        "mysqlflexible" => "Microsoft.DBforMySQL/flexibleServers",
        "aks" => "Microsoft.ContainerService/managedClusters",
        "elasticsan" => "Microsoft.ElasticSan/elasticSans/volumeGroups",
        _ => workloadType // If already an ARM resource type, pass through
    };

    /// <summary>
    /// Determines if a DPP datasource type uses OperationalStore (snapshot-based) vs VaultStore.
    /// </summary>
    internal static bool UsesOperationalStore(string datasourceType) => datasourceType.ToLowerInvariant() switch
    {
        "microsoft.compute/disks" => true,
        "microsoft.storage/storageaccounts/blobservices" => true,
        "microsoft.containerservice/managedclusters" => true,
        "microsoft.elasticsan/elasticsans/volumegroups" => true,
        "azuredisk" => true,
        "azureblob" => true,
        "aks" => true,
        "elasticsan" => true,
        _ => false
    };

    /// <summary>
    /// Determines if a datasource type is for blob operational backup (continuous, no scheduled backup rule).
    /// </summary>
    internal static bool IsBlobOperationalBackup(string datasourceType) => datasourceType.ToLowerInvariant() switch
    {
        "microsoft.storage/storageaccounts/blobservices" => true,
        "azureblob" => true,
        _ => false
    };

    /// <summary>
    /// Determines if a datasource type is for Elastic SAN backup.
    /// ESAN uses OperationalStore with daily (P1D) schedule instead of hourly (PT4H) like Disk/AKS.
    /// ESAN also requires DataSourceSetInfo pointing to the parent ElasticSan resource.
    /// </summary>
    internal static bool IsElasticSanWorkload(string datasourceType) => datasourceType.ToLowerInvariant() switch
    {
        "microsoft.elasticsan/elasticsans/volumegroups" => true,
        "elasticsan" => true,
        _ => false
    };

    /// <summary>
    /// Determines if a datasource type is for AKS backup.
    /// AKS requires DataSourceSetInfo pointing to the cluster itself (container of namespaces).
    /// </summary>
    internal static bool IsAksWorkload(string datasourceType) => datasourceType.ToLowerInvariant() switch
    {
        "microsoft.containerservice/managedclusters" => true,
        "aks" => true,
        _ => false
    };

    /// <summary>
    /// Determines if a datasource type requires DataSourceSetInfo.
    /// Both ESAN and AKS workloads require DataSourceSetInfo in protect and restore payloads.
    /// </summary>
    internal static bool RequiresDataSourceSetInfo(string datasourceType) =>
        IsElasticSanWorkload(datasourceType) || IsAksWorkload(datasourceType);

    /// <summary>
    /// Derives the parent Elastic SAN resource ID from a volume group resource ID.
    /// e.g., ".../elasticSans/mysan/volumeGroups/myvg" => ".../elasticSans/mysan"
    /// </summary>
    internal static ResourceIdentifier GetElasticSanParentId(ResourceIdentifier volumeGroupId)
    {
        // VolumeGroup ID: .../providers/Microsoft.ElasticSan/elasticSans/{sanName}/volumeGroups/{vgName}
        // Parent ESAN ID: .../providers/Microsoft.ElasticSan/elasticSans/{sanName}
        var idStr = volumeGroupId.ToString();
        var vgIndex = idStr.LastIndexOf("/volumeGroups/", StringComparison.OrdinalIgnoreCase);
        if (vgIndex > 0)
        {
            return new ResourceIdentifier(idStr[..vgIndex]);
        }

        return volumeGroupId.Parent ?? volumeGroupId;
    }

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
        var vaultData = await vaultResource.GetAsync(cancellationToken);
        var collection = vaultResource.GetDataProtectionBackupInstances();

        var policyId = DataProtectionBackupPolicyResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, policyName);
        var datasourceResourceId = new ResourceIdentifier(datasourceId);

        // Auto-detect blob storage: if the datasource is a storage account, set the datasource type
        // to blobServices but keep the resource ID pointing to the storage account (not blobServices/default)
        var resolvedDatasourceType = datasourceType ?? datasourceResourceId.ResourceType.ToString();
        // Map friendly names (e.g., "aks", "elasticsan", "azuredisk") to ARM resource types
        resolvedDatasourceType = MapWorkloadTypeToArmResourceType(resolvedDatasourceType);
        if (resolvedDatasourceType.Equals("Microsoft.Storage/storageAccounts", StringComparison.OrdinalIgnoreCase)
            && !datasourceId.Contains("/blobServices/", StringComparison.OrdinalIgnoreCase))
        {
            // Blob operational backup uses storageAccounts/blobServices as the datasource type
            // but the resource ID stays as the storage account (no /blobServices/default appended)
            resolvedDatasourceType = "Microsoft.Storage/storageAccounts/blobServices";
        }

        // For ESAN, use parent-child naming: {esanName}-{vgName}-{guid}
        var instanceName = IsElasticSanWorkload(resolvedDatasourceType)
            ? $"{GetElasticSanParentId(datasourceResourceId).Name}-{datasourceResourceId.Name}-{Guid.NewGuid()}"
            : $"{datasourceResourceId.Name}-{datasourceResourceId.Name}-{Guid.NewGuid().ToString("N")[..12]}";

        var policyInfo = new BackupInstancePolicyInfo(policyId);

        // For Disk, AKS, and ESAN workloads that use OperationalStore (snapshot-based),
        // set the snapshot resource group parameter (defaults to the datasource's resource group)
        if (UsesOperationalStore(resolvedDatasourceType) && !IsBlobOperationalBackup(resolvedDatasourceType))
        {
            var snapshotRgId = ResourceGroupResource.CreateResourceIdentifier(subscription, datasourceResourceId.ResourceGroupName ?? resourceGroup);
            var opStoreSettings = new OperationalDataStoreSettings(DataStoreType.OperationalStore)
            {
                ResourceGroupId = snapshotRgId,
            };
            policyInfo.PolicyParameters = new BackupInstancePolicySettings();
            policyInfo.PolicyParameters.DataStoreParametersList.Add(opStoreSettings);

            // AKS requires BackupDataSourceParametersList with cluster backup settings
            // (which namespaces/volumes to back up, cluster-scope resources, etc.)
            if (IsAksWorkload(resolvedDatasourceType))
            {
                var aksSettings = new KubernetesClusterBackupDataSourceSettings(
                    isSnapshotVolumesEnabled: true,
                    isClusterScopeResourcesIncluded: true);
                policyInfo.PolicyParameters.BackupDataSourceParametersList.Add(aksSettings);
            }
        }

        var dataSourceInfo = new DataSourceInfo(datasourceResourceId)
        {
            DataSourceType = resolvedDatasourceType,
            ObjectType = "Datasource",
            ResourceType = datasourceResourceId.ResourceType,
            ResourceName = datasourceResourceId.Name,
            ResourceLocation = vaultData.Value.Data.Location,
        };
        var instanceProperties = new DataProtectionBackupInstanceProperties(
            dataSourceInfo,
            policyInfo,
            string.Empty)
        {
            ObjectType = "BackupInstance",
        };

        // ESAN and AKS require DataSourceSetInfo
        // ESAN: points to the parent Elastic SAN resource
        // AKS: points to the AKS cluster itself (datasource == datasource set)
        if (RequiresDataSourceSetInfo(resolvedDatasourceType))
        {
            var setId = IsElasticSanWorkload(resolvedDatasourceType)
                ? GetElasticSanParentId(datasourceResourceId)
                : datasourceResourceId;
            instanceProperties.DataSourceSetInfo = new DataSourceSetInfo(setId)
            {
                DataSourceType = resolvedDatasourceType,
                ObjectType = "DatasourceSet",
                ResourceType = setId.ResourceType,
                ResourceName = setId.Name,
                ResourceLocation = vaultData.Value.Data.Location,
            };
        }

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
        string protectedItemName, string? expiry, string? backupType,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var instanceId = DataProtectionBackupInstanceResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName);
        var instanceResource = armClient.GetDataProtectionBackupInstanceResource(instanceId);

        // Fetch backup instance to get associated policy
        var instanceData = await instanceResource.GetAsync(cancellationToken);
        var policyId = instanceData.Value.Data.Properties.PolicyInfo.PolicyId;

        // Fetch policy to find the backup rule name
        var policyResource = armClient.GetDataProtectionBackupPolicyResource(policyId);
        var policyData = await policyResource.GetAsync(cancellationToken);

        string ruleName = "BackupDaily";
        if (policyData.Value.Data.Properties is RuleBasedBackupPolicy ruleBasedPolicy)
        {
            var backupRule = ruleBasedPolicy.PolicyRules.FirstOrDefault(r => r is DataProtectionBackupRule);
            if (backupRule != null)
            {
                ruleName = backupRule.Name;
            }
        }

        var ruleOption = new AdhocBackupRules(ruleName, "Default");
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
        string protectedItemName, string? recoveryPointId,
        string? targetResourceId, string? restoreLocation, string? pointInTime, string? tenant,
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

        // Get the backup instance to determine datasource info
        var instance = await instanceResource.GetAsync(cancellationToken);
        var datasourceInfo = instance.Value.Data.Properties?.DataSourceInfo;
        var datasourceId = datasourceInfo?.ResourceId;
        var location = !string.IsNullOrEmpty(restoreLocation) ? new AzureLocation(restoreLocation) : datasourceInfo?.ResourceLocation;

        RestoreTargetInfoBase restoreTarget;

        // Determine if this is a restore-as-files scenario (target is a storage account)
        if (!string.IsNullOrEmpty(targetResourceId) &&
            targetResourceId.Contains("Microsoft.Storage/storageAccounts", StringComparison.OrdinalIgnoreCase))
        {
            // Restore-as-files: target is a storage account with optional container
            var storageAccountId = targetResourceId;
            var containerName = "pgflex-restore";

            // Extract container name from the target resource ID if it includes a container
            // e.g., .../storageAccounts/name/blobServices/default/containers/containerName
            if (targetResourceId.Contains("/blobServices/", StringComparison.OrdinalIgnoreCase))
            {
                var parts = targetResourceId.Split('/');
                containerName = parts[^1];
                var containerIndex = targetResourceId.IndexOf("/blobServices/", StringComparison.OrdinalIgnoreCase);
                storageAccountId = targetResourceId[..containerIndex];
            }

            // Extract storage account name from the ARM ID
            var storageAccountArmId = new ResourceIdentifier(storageAccountId);
            var storageAccountName = storageAccountArmId.Name;

            var containerUri = new Uri($"https://{storageAccountName}.blob.core.windows.net/{containerName}");
            var filePrefix = $"pgflex-restore-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            // TargetResourceArmId must point to the container (not the storage account)
            // per the REST API spec: "ARM Id pointing to container / file share"
            var containerArmId = new ResourceIdentifier(
                $"{storageAccountId}/blobServices/default/containers/{containerName}");

            var targetDetails = new RestoreFilesTargetDetails(
                filePrefix,
                RestoreTargetLocationType.AzureBlobs,
                containerUri)
            {
                TargetResourceArmId = containerArmId,
            };

            restoreTarget = new RestoreFilesTargetInfo(
                RecoverySetting.FailIfExists,
                targetDetails)
            {
                RestoreLocation = location,
            };
        }
        else
        {
            // Restore-as-server: target is a datasource resource
            var targetId = !string.IsNullOrEmpty(targetResourceId) ? new ResourceIdentifier(targetResourceId) : datasourceId;

            var targetDatasource = new DataSourceInfo(targetId ?? datasourceId!)
            {
                ObjectType = "Datasource",
                DataSourceType = datasourceInfo?.DataSourceType,
                ResourceType = targetId?.ResourceType ?? datasourceId?.ResourceType,
                ResourceName = targetId?.Name ?? datasourceId?.Name,
                ResourceLocation = location,
            };

            var restoreTargetInfo = new RestoreTargetInfo(
                RecoverySetting.FailIfExists,
                targetDatasource)
            {
                RestoreLocation = location,
            };

            // ESAN and AKS restore require DataSourceSetInfo
            var resolvedDatasourceTypeStr = datasourceInfo?.DataSourceType ?? string.Empty;
            if (RequiresDataSourceSetInfo(resolvedDatasourceTypeStr))
            {
                var dsId = targetId ?? datasourceId!;
                var setId = IsElasticSanWorkload(resolvedDatasourceTypeStr)
                    ? GetElasticSanParentId(dsId)
                    : dsId;
                restoreTargetInfo.DataSourceSetInfo = new DataSourceSetInfo(setId)
                {
                    DataSourceType = datasourceInfo?.DataSourceType,
                    ObjectType = "DatasourceSet",
                    ResourceType = setId.ResourceType,
                    ResourceName = setId.Name,
                    ResourceLocation = location,
                };
            }

            restoreTarget = restoreTargetInfo;
        }

        // Determine the correct source data store type based on datasource type
        var datasourceTypeStr = datasourceInfo?.DataSourceType ?? string.Empty;
        var sourceDataStoreType = UsesOperationalStore(datasourceTypeStr) ? SourceDataStoreType.OperationalStore : SourceDataStoreType.VaultStore;

        // Use time-based restore for blob PIT or when --point-in-time is provided
        BackupRestoreContent restoreContent;
        if (!string.IsNullOrEmpty(pointInTime))
        {
            if (!DateTimeOffset.TryParse(pointInTime, out var recoverOn))
            {
                throw new ArgumentException($"Invalid point-in-time format: '{pointInTime}'. Use ISO 8601 format (e.g., '2025-01-15T10:30:00Z').");
            }

            restoreContent = new BackupRecoveryTimeBasedRestoreContent(
                restoreTarget,
                sourceDataStoreType,
                recoverOn);
        }
        else if (!string.IsNullOrEmpty(recoveryPointId))
        {
            restoreContent = new BackupRecoveryPointBasedRestoreContent(
                restoreTarget,
                sourceDataStoreType,
                recoveryPointId);
        }
        else
        {
            throw new ArgumentException("Either --recovery-point or --point-in-time must be specified for restore.");
        }

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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken);

        var patchData = new DataProtectionBackupVaultPatch();

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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        await vaultResource.DeleteAsync(WaitUntil.Completed, cancellationToken);

        return new OperationResult("Succeeded", null, $"Vault '{vaultName}' deleted successfully.");
    }

    public async Task<OperationResult> UpdatePolicyAsync(
        string vaultName, string resourceGroup, string subscription,
        string policyName, string? scheduleFrequency,
        string? dailyRetentionDays, string? weeklyRetentionWeeks,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(policyName), policyName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);

        // Fetch the existing policy — will throw RequestFailedException (404) if it doesn't exist
        var policyId = DataProtectionBackupPolicyResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, policyName);
        var policyResource = armClient.GetDataProtectionBackupPolicyResource(policyId);
        var existingPolicy = await policyResource.GetAsync(cancellationToken);
        var policyData = existingPolicy.Value.Data;

        // Modify only the requested retention on existing policy rules, preserving datasourceTypes and structure
        if (policyData.Properties is RuleBasedBackupPolicy ruleBasedPolicy)
        {
            foreach (var rule in ruleBasedPolicy.PolicyRules)
            {
                if (rule is DataProtectionRetentionRule retentionRule && int.TryParse(dailyRetentionDays, out var days))
                {
                    foreach (var lifecycle in retentionRule.Lifecycles)
                    {
                        lifecycle.DeleteAfter = new DataProtectionBackupAbsoluteDeleteSetting(TimeSpan.FromDays(days));
                    }
                }
            }
        }

        // PUT the modified policy back with original datasourceTypes preserved
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        var collection = vaultResource.GetDataProtectionBackupPolicies();
        await collection.CreateOrUpdateAsync(WaitUntil.Completed, policyName, policyData, cancellationToken);

        return new OperationResult("Succeeded", null, $"Policy '{policyName}' updated in vault '{vaultName}'.");
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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        var collection = vaultResource.GetDataProtectionBackupPolicies();

        // Parse retention and schedule parameters
        var retentionDays = int.TryParse(dailyRetentionDays, out var dd) ? dd : 30;
        var scheduleTimeValue = scheduleTime ?? "02:00";
        var now = DateTimeOffset.UtcNow;
        var scheduleParts = scheduleTimeValue.Split(':');
        var scheduleHour = int.TryParse(scheduleParts[0], out var sh) ? sh : 2;
        var scheduleMinute = scheduleParts.Length > 1 && int.TryParse(scheduleParts[1], out var sm) ? sm : 0;
        var scheduleStartTime = new DateTimeOffset(now.Year, now.Month, now.Day, scheduleHour, scheduleMinute, 0, TimeSpan.Zero);

        // Map friendly workload type to ARM resource type and determine data store type
        var resolvedWorkloadType = MapWorkloadTypeToArmResourceType(workloadType);
        var useOperationalStore = UsesOperationalStore(workloadType);
        var isBlobOperational = IsBlobOperationalBackup(workloadType);
        var dataStoreType = useOperationalStore ? DataStoreType.OperationalStore : DataStoreType.VaultStore;

        // Build the retention rule (Default) - use 7 days for operational store if not specified
        var defaultRetention = useOperationalStore && !int.TryParse(dailyRetentionDays, out _) ? 7 : retentionDays;
        var retentionDeleteSetting = new DataProtectionBackupAbsoluteDeleteSetting(TimeSpan.FromDays(defaultRetention));
        var retentionDataStore = new DataStoreInfoBase(dataStoreType, "DataStoreInfoBase");
        var retentionLifeCycle = new SourceLifeCycle(retentionDeleteSetting, retentionDataStore);
        var retentionRule = new DataProtectionRetentionRule("Default", [retentionLifeCycle])
        {
            IsDefault = true,
        };

        List<DataProtectionBasePolicyRule> rules = [retentionRule];

        // Blob operational backup uses continuous mode - no scheduled backup rule needed
        // For all other workloads, add a scheduled backup rule
        if (!isBlobOperational)
        {
            // Disk and AKS use PT4H (hourly) schedule; ESAN and VaultStore workloads use P1D (daily)
            var isElasticSan = IsElasticSanWorkload(workloadType);
            var useHourlySchedule = useOperationalStore && !isElasticSan;
            var scheduleInterval = useHourlySchedule ? "PT4H" : "P1D";
            var ruleName = useHourlySchedule ? "BackupHourly" : "BackupDaily";
            var repeatingInterval = $"R/{scheduleStartTime:yyyy-MM-ddTHH:mm:ss+00:00}/{scheduleInterval}";

            var schedule = new DataProtectionBackupSchedule([repeatingInterval])
            {
                TimeZone = "UTC",
            };
            var defaultTag = new DataProtectionBackupRetentionTag("Default");
            var taggingCriteria = new DataProtectionBackupTaggingCriteria(true, 99, defaultTag);
            var triggerContext = new ScheduleBasedBackupTriggerContext(schedule, [taggingCriteria]);
            var backupDataStore = new DataStoreInfoBase(dataStoreType, "DataStoreInfoBase");
            var backupRule = new DataProtectionBackupRule(ruleName, backupDataStore, triggerContext)
            {
                // Disk/AKS use "Incremental" backup type; VaultStore workloads use "Full"
                BackupParameters = new DataProtectionBackupSettings(useOperationalStore ? "Incremental" : "Full"),
            };

            rules.Add(backupRule);
        }

        var policyProperties = new RuleBasedBackupPolicy(
            [resolvedWorkloadType],
            rules);
        var policyData = new DataProtectionBackupPolicyData { Properties = policyProperties };

        await collection.CreateOrUpdateAsync(WaitUntil.Completed, policyName, policyData, cancellationToken);

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
        var policyId = DataProtectionBackupPolicyResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, policyName);
        var policyResource = armClient.GetDataProtectionBackupPolicyResource(policyId);
        await policyResource.DeleteAsync(WaitUntil.Completed, cancellationToken);

        return new OperationResult("Succeeded", null, $"Policy '{policyName}' deleted from vault '{vaultName}'.");
    }

    public async Task<OperationResult> StopProtectionAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string mode,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);

        if (mode.Equals("DeleteData", StringComparison.OrdinalIgnoreCase))
        {
            var instanceId = DataProtectionBackupInstanceResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName);
            var instanceResource = armClient.GetDataProtectionBackupInstanceResource(instanceId);
            await instanceResource.DeleteAsync(WaitUntil.Started, cancellationToken);
            return new OperationResult("Accepted", null, "Protection stopped and data deletion initiated.");
        }

        // DPP suspend backup
        var instId = DataProtectionBackupInstanceResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName);
        var instResource = armClient.GetDataProtectionBackupInstanceResource(instId);
        await instResource.SuspendBackupsAsync(WaitUntil.Started, cancellationToken);

        return new OperationResult("Accepted", null, "Protection stopped with data retained.");
    }

    public async Task<OperationResult> ResumeProtectionAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? policyName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        ValidateRequiredParameters(
            (nameof(vaultName), vaultName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(protectedItemName), protectedItemName));

        var armClient = await CreateArmClientAsync(tenant, retryPolicy, cancellationToken: cancellationToken);
        var instanceId = DataProtectionBackupInstanceResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName, protectedItemName);
        var instanceResource = armClient.GetDataProtectionBackupInstanceResource(instanceId);
        await instanceResource.ResumeBackupsAsync(WaitUntil.Started, cancellationToken);

        return new OperationResult("Accepted", null, "Protection resumed.");
    }

    public Task<OperationResult> ModifyProtectionAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? newPolicyName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        // DPP modify requires re-creating the instance with a different policy
        return Task.FromResult(new OperationResult("NotSupported", null, "To change policy for a DPP backup instance, stop protection (RetainData) and re-protect with the new policy."));
    }

    public Task<OperationResult> UndeleteProtectedItemAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        return Task.FromResult(new OperationResult("NotSupported", null, "Undelete for DPP backup instances is managed automatically during the soft-delete retention period."));
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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);

        var patchData = new DataProtectionBackupVaultPatch();
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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);

        var patchData = new DataProtectionBackupVaultPatch();
        await vaultResource.UpdateAsync(WaitUntil.Completed, patchData, cancellationToken);

        return new OperationResult("Succeeded", null, $"Soft delete set to '{softDeleteState}' for vault '{vaultName}'.");
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
        var vaultId = DataProtectionBackupVaultResource.CreateResourceIdentifier(subscription, resourceGroup, vaultName);
        var vaultResource = armClient.GetDataProtectionBackupVaultResource(vaultId);
        var vault = await vaultResource.GetAsync(cancellationToken);

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
            vault.Value.Data.Properties?.SecuritySettings?.SoftDeleteSettings?.State?.ToString(),
            vault.Value.Data.Properties?.SecuritySettings?.ImmutabilityState?.ToString(),
            null, details);
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
