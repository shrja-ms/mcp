// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Core.Services.Azure.Tenant;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.ResourceManager.DataProtectionBackup;
using Azure.ResourceManager.DataProtectionBackup.Models;

namespace Azure.Mcp.Tools.DataProtection.Services;

public class DataProtectionService(ISubscriptionService subscriptionService, ITenantService tenantService)
    : BaseAzureService(tenantService), IDataProtectionService
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;

    public async Task<IEnumerable<BackupVaultModel>> ListVaultsAsync(
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters((nameof(subscription), subscription));

        var subscriptionResource = await _subscriptionService.GetSubscription(subscription, tenant, retryPolicy, cancellationToken)
            ?? throw new Exception($"Subscription '{subscription}' not found");

        var vaults = new List<BackupVaultModel>();
        await foreach (var vault in subscriptionResource.GetDataProtectionBackupVaultsAsync(cancellationToken))
        {
            vaults.Add(MapVault(vault));
        }

        return vaults;
    }

    public async Task<BackupVaultModel> GetVaultAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault));

        var subscriptionResource = await _subscriptionService.GetSubscription(subscription, tenant, retryPolicy, cancellationToken)
            ?? throw new Exception($"Subscription '{subscription}' not found");

        var rg = await subscriptionResource.GetResourceGroups().GetAsync(resourceGroup, cancellationToken);
        var vaultResource = await rg.Value.GetDataProtectionBackupVaultAsync(vault, cancellationToken);

        return MapVault(vaultResource.Value);
    }

    public async Task<IEnumerable<BackupInstanceModel>> ListBackupInstancesAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault));

        var vaultResource = await GetVaultResourceAsync(vault, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);

        var instances = new List<BackupInstanceModel>();
        await foreach (var instance in vaultResource.GetDataProtectionBackupInstances().GetAllAsync(cancellationToken))
        {
            instances.Add(MapBackupInstance(instance));
        }

        return instances;
    }

    public async Task<BackupInstanceModel> GetBackupInstanceAsync(
        string backupInstance,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault),
            (nameof(backupInstance), backupInstance));

        var vaultResource = await GetVaultResourceAsync(vault, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
        var instanceResource = await vaultResource.GetDataProtectionBackupInstanceAsync(backupInstance, cancellationToken);

        return MapBackupInstance(instanceResource.Value);
    }

    public async Task<IEnumerable<BackupPolicyModel>> ListPoliciesAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault));

        var vaultResource = await GetVaultResourceAsync(vault, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);

        var policies = new List<BackupPolicyModel>();
        await foreach (var policy in vaultResource.GetDataProtectionBackupPolicies().GetAllAsync(cancellationToken))
        {
            policies.Add(MapPolicy(policy));
        }

        return policies;
    }

    public async Task<BackupPolicyModel> GetPolicyAsync(
        string policy,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault),
            (nameof(policy), policy));

        var vaultResource = await GetVaultResourceAsync(vault, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
        var policyResource = await vaultResource.GetDataProtectionBackupPolicyAsync(policy, cancellationToken);

        return MapPolicy(policyResource.Value);
    }

    public async Task<IEnumerable<BackupJobModel>> ListJobsAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault));

        var vaultResource = await GetVaultResourceAsync(vault, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);

        var jobs = new List<BackupJobModel>();
        await foreach (var job in vaultResource.GetDataProtectionBackupJobs().GetAllAsync(cancellationToken))
        {
            jobs.Add(MapJob(job));
        }

        return jobs;
    }

    public async Task<BackupJobModel> GetJobAsync(
        string job,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault),
            (nameof(job), job));

        var vaultResource = await GetVaultResourceAsync(vault, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
        var jobResource = await vaultResource.GetDataProtectionBackupJobAsync(job, cancellationToken);

        return MapJob(jobResource.Value);
    }

    public async Task<IEnumerable<RecoveryPointModel>> ListRecoveryPointsAsync(
        string backupInstance,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault),
            (nameof(backupInstance), backupInstance));

        var vaultResource = await GetVaultResourceAsync(vault, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
        var instanceResource = await vaultResource.GetDataProtectionBackupInstanceAsync(backupInstance, cancellationToken);

        var recoveryPoints = new List<RecoveryPointModel>();
        await foreach (var rp in instanceResource.Value.GetDataProtectionBackupRecoveryPoints().GetAllAsync(cancellationToken: cancellationToken))
        {
            recoveryPoints.Add(MapRecoveryPoint(rp));
        }

        return recoveryPoints;
    }

    public async Task<RecoveryPointModel> GetRecoveryPointAsync(
        string recoveryPoint,
        string backupInstance,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(subscription), subscription),
            (nameof(resourceGroup), resourceGroup),
            (nameof(vault), vault),
            (nameof(backupInstance), backupInstance),
            (nameof(recoveryPoint), recoveryPoint));

        var vaultResource = await GetVaultResourceAsync(vault, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
        var instanceResource = await vaultResource.GetDataProtectionBackupInstanceAsync(backupInstance, cancellationToken);
        var rpResource = await instanceResource.Value.GetDataProtectionBackupRecoveryPointAsync(recoveryPoint, cancellationToken);

        return MapRecoveryPoint(rpResource.Value);
    }

    private async Task<DataProtectionBackupVaultResource> GetVaultResourceAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant,
        RetryPolicyOptions? retryPolicy,
        CancellationToken cancellationToken)
    {
        var subscriptionResource = await _subscriptionService.GetSubscription(subscription, tenant, retryPolicy, cancellationToken)
            ?? throw new Exception($"Subscription '{subscription}' not found");

        var rg = await subscriptionResource.GetResourceGroups().GetAsync(resourceGroup, cancellationToken);
        var vaultResource = await rg.Value.GetDataProtectionBackupVaultAsync(vault, cancellationToken);

        return vaultResource.Value;
    }

    private static BackupVaultModel MapVault(DataProtectionBackupVaultResource vaultResource)
    {
        var data = vaultResource.Data;
        return new BackupVaultModel
        {
            Name = data.Name,
            ResourceGroup = vaultResource.Id.ResourceGroupName,
            Location = data.Location.ToString(),
            ProvisioningState = data.Properties?.ProvisioningState?.ToString(),
            StorageType = data.Properties?.StorageSettings?.FirstOrDefault()?.DataStoreType?.ToString(),
            SoftDeleteState = data.Properties?.SecuritySettings?.SoftDeleteSettings?.State?.ToString(),
            Tags = data.Tags?.Count > 0 ? data.Tags : null
        };
    }

    private static BackupInstanceModel MapBackupInstance(DataProtectionBackupInstanceResource instanceResource)
    {
        var data = instanceResource.Data;
        var props = data.Properties;
        return new BackupInstanceModel
        {
            Name = data.Name,
            DataSourceType = props?.DataSourceInfo?.DataSourceType,
            DataSourceId = props?.DataSourceInfo?.ResourceId?.ToString(),
            PolicyName = props?.PolicyInfo?.PolicyId?.ToString()?.Split('/')?.LastOrDefault(),
            ProtectionStatus = props?.ProtectionStatus?.Status?.ToString(),
            CurrentProtectionState = props?.CurrentProtectionState?.ToString(),
            ProvisioningState = props?.ProvisioningState?.ToString()
        };
    }

    private static BackupPolicyModel MapPolicy(DataProtectionBackupPolicyResource policyResource)
    {
        var data = policyResource.Data;
        var model = new BackupPolicyModel
        {
            Name = data.Name,
        };

        if (data.Properties is RuleBasedBackupPolicy backupPolicy)
        {
            model.DataSourceType = backupPolicy.DataSourceTypes?.FirstOrDefault();
            model.DataStoreTypes = backupPolicy.PolicyRules?
                .OfType<DataProtectionBackupRule>()
                .Select(r => r.DataStore?.DataStoreType.ToString())
                .Where(t => t != null)
                .Cast<string>()
                .Distinct()
                .ToList();
        }

        return model;
    }

    private static BackupJobModel MapJob(DataProtectionBackupJobResource jobResource)
    {
        var data = jobResource.Data;
        var props = data.Properties;
        return new BackupJobModel
        {
            Name = data.Name,
            Operation = props?.Operation,
            Status = props?.Status,
            DataSourceName = props?.DataSourceName,
            DataSourceType = props?.DataSourceType,
            StartTime = props?.StartOn,
            EndTime = props?.EndOn,
            Duration = props?.Duration?.ToString(),
            IsUserTriggered = props?.IsUserTriggered
        };
    }

    private static RecoveryPointModel MapRecoveryPoint(DataProtectionBackupRecoveryPointResource rpResource)
    {
        var data = rpResource.Data;
        var model = new RecoveryPointModel
        {
            Name = data.Name,
        };

        if (data.Properties is DataProtectionBackupDiscreteRecoveryPointProperties discreteRp)
        {
            model.RecoveryPointType = discreteRp.RecoveryPointType;
            model.RecoveryPointTime = discreteRp.RecoverOn;
        }

        return model;
    }
}
