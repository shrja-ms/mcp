// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Models;

namespace Azure.Mcp.Tools.AzureBackup.Services;

public class AzureBackupService(IRsvBackupOperations rsvOps, IDppBackupOperations dppOps) : IAzureBackupService
{
    public async Task<VaultCreateResult> CreateVaultAsync(
        string vaultName, string resourceGroup, string subscription, string vaultType,
        string location, string? sku, string? storageType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        VaultTypeResolver.ValidateVaultType(vaultType);

        return VaultTypeResolver.IsRsv(vaultType)
            ? await rsvOps.CreateVaultAsync(vaultName, resourceGroup, subscription, location, sku, storageType, tenant, retryPolicy, cancellationToken)
            : await dppOps.CreateVaultAsync(vaultName, resourceGroup, subscription, location, sku, storageType, tenant, retryPolicy, cancellationToken);
    }

    public async Task<BackupVaultInfo> GetVaultAsync(
        string vaultName, string resourceGroup, string subscription,
        string? vaultType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        if (VaultTypeResolver.IsVaultTypeSpecified(vaultType))
        {
            return VaultTypeResolver.IsRsv(vaultType)
                ? await rsvOps.GetVaultAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken)
                : await dppOps.GetVaultAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
        }

        // Auto-detect: try RSV first, then DPP
        return await AutoDetectAndExecuteAsync(
            () => rsvOps.GetVaultAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken),
            () => dppOps.GetVaultAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken),
            vaultName);
    }

    public async Task<List<BackupVaultInfo>> ListVaultsAsync(
        string subscription, string? vaultType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        if (VaultTypeResolver.IsRsv(vaultType))
        {
            return await rsvOps.ListVaultsAsync(subscription, tenant, retryPolicy, cancellationToken);
        }

        if (VaultTypeResolver.IsDpp(vaultType))
        {
            return await dppOps.ListVaultsAsync(subscription, tenant, retryPolicy, cancellationToken);
        }

        // List both types and merge
        var rsvTask = rsvOps.ListVaultsAsync(subscription, tenant, retryPolicy, cancellationToken);
        var dppTask = dppOps.ListVaultsAsync(subscription, tenant, retryPolicy, cancellationToken);

        await Task.WhenAll(rsvTask, dppTask);

        var merged = new List<BackupVaultInfo>();
        merged.AddRange(await rsvTask);
        merged.AddRange(await dppTask);
        return merged;
    }

    public async Task<ProtectResult> ProtectItemAsync(
        string vaultName, string resourceGroup, string subscription,
        string datasourceId, string policyName, string? vaultType,
        string? containerName, string? datasourceType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.ProtectItemAsync(vaultName, resourceGroup, subscription, datasourceId, policyName, containerName, datasourceType, tenant, retryPolicy, cancellationToken)
            : await dppOps.ProtectItemAsync(vaultName, resourceGroup, subscription, datasourceId, policyName, datasourceType, tenant, retryPolicy, cancellationToken);
    }

    public async Task<ProtectedItemInfo> GetProtectedItemAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? vaultType, string? containerName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.GetProtectedItemAsync(vaultName, resourceGroup, subscription, protectedItemName, containerName, tenant, retryPolicy, cancellationToken)
            : await dppOps.GetProtectedItemAsync(vaultName, resourceGroup, subscription, protectedItemName, tenant, retryPolicy, cancellationToken);
    }

    public async Task<List<ProtectedItemInfo>> ListProtectedItemsAsync(
        string vaultName, string resourceGroup, string subscription,
        string? vaultType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.ListProtectedItemsAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken)
            : await dppOps.ListProtectedItemsAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
    }

    public async Task<BackupTriggerResult> TriggerBackupAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? vaultType, string? containerName,
        string? expiry, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.TriggerBackupAsync(vaultName, resourceGroup, subscription, protectedItemName, containerName, expiry, tenant, retryPolicy, cancellationToken)
            : await dppOps.TriggerBackupAsync(vaultName, resourceGroup, subscription, protectedItemName, expiry, tenant, retryPolicy, cancellationToken);
    }

    public async Task<RestoreTriggerResult> TriggerRestoreAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string recoveryPointId, string? vaultType,
        string? containerName, string? targetResourceId, string? restoreLocation,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.TriggerRestoreAsync(vaultName, resourceGroup, subscription, protectedItemName, recoveryPointId, containerName, targetResourceId, restoreLocation, tenant, retryPolicy, cancellationToken)
            : await dppOps.TriggerRestoreAsync(vaultName, resourceGroup, subscription, protectedItemName, recoveryPointId, targetResourceId, restoreLocation, tenant, retryPolicy, cancellationToken);
    }

    public async Task<BackupPolicyInfo> GetPolicyAsync(
        string vaultName, string resourceGroup, string subscription,
        string policyName, string? vaultType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.GetPolicyAsync(vaultName, resourceGroup, subscription, policyName, tenant, retryPolicy, cancellationToken)
            : await dppOps.GetPolicyAsync(vaultName, resourceGroup, subscription, policyName, tenant, retryPolicy, cancellationToken);
    }

    public async Task<List<BackupPolicyInfo>> ListPoliciesAsync(
        string vaultName, string resourceGroup, string subscription,
        string? vaultType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.ListPoliciesAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken)
            : await dppOps.ListPoliciesAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
    }

    public async Task<BackupJobInfo> GetJobAsync(
        string vaultName, string resourceGroup, string subscription,
        string jobId, string? vaultType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.GetJobAsync(vaultName, resourceGroup, subscription, jobId, tenant, retryPolicy, cancellationToken)
            : await dppOps.GetJobAsync(vaultName, resourceGroup, subscription, jobId, tenant, retryPolicy, cancellationToken);
    }

    public async Task<List<BackupJobInfo>> ListJobsAsync(
        string vaultName, string resourceGroup, string subscription,
        string? vaultType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.ListJobsAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken)
            : await dppOps.ListJobsAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
    }

    public async Task<RecoveryPointInfo> GetRecoveryPointAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string recoveryPointId, string? vaultType,
        string? containerName, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.GetRecoveryPointAsync(vaultName, resourceGroup, subscription, protectedItemName, recoveryPointId, containerName, tenant, retryPolicy, cancellationToken)
            : await dppOps.GetRecoveryPointAsync(vaultName, resourceGroup, subscription, protectedItemName, recoveryPointId, tenant, retryPolicy, cancellationToken);
    }

    public async Task<List<RecoveryPointInfo>> ListRecoveryPointsAsync(
        string vaultName, string resourceGroup, string subscription,
        string protectedItemName, string? vaultType, string? containerName,
        string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        var resolvedType = await ResolveVaultTypeAsync(vaultName, resourceGroup, subscription, vaultType, tenant, retryPolicy, cancellationToken);

        return VaultTypeResolver.IsRsv(resolvedType)
            ? await rsvOps.ListRecoveryPointsAsync(vaultName, resourceGroup, subscription, protectedItemName, containerName, tenant, retryPolicy, cancellationToken)
            : await dppOps.ListRecoveryPointsAsync(vaultName, resourceGroup, subscription, protectedItemName, tenant, retryPolicy, cancellationToken);
    }

    private async Task<string> ResolveVaultTypeAsync(
        string vaultName, string resourceGroup, string subscription,
        string? vaultType, string? tenant,
        RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken)
    {
        if (VaultTypeResolver.IsVaultTypeSpecified(vaultType))
        {
            return vaultType!;
        }

        // Auto-detect by trying RSV first, then DPP
        try
        {
            await rsvOps.GetVaultAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
            return VaultTypeResolver.Rsv;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Not an RSV vault, try DPP
        }

        try
        {
            await dppOps.GetVaultAsync(vaultName, resourceGroup, subscription, tenant, retryPolicy, cancellationToken);
            return VaultTypeResolver.Dpp;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new KeyNotFoundException($"Vault '{vaultName}' not found in resource group '{resourceGroup}'. Verify the vault name and resource group, or specify --vault-type explicitly.");
        }
    }

    private static async Task<T> AutoDetectAndExecuteAsync<T>(
        Func<Task<T>> rsvAction, Func<Task<T>> dppAction, string vaultName)
    {
        try
        {
            return await rsvAction();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Not found in RSV, try DPP
        }

        try
        {
            return await dppAction();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new KeyNotFoundException($"Vault '{vaultName}' not found as either RSV or DPP vault. Verify the vault name and resource group, or specify --vault-type explicitly.");
        }
    }
}
