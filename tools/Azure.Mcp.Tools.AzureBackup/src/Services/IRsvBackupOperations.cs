// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Models;

namespace Azure.Mcp.Tools.AzureBackup.Services;

public interface IRsvBackupOperations
{
    Task<VaultCreateResult> CreateVaultAsync(string vaultName, string resourceGroup, string subscription, string location, string? sku, string? storageType, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<BackupVaultInfo> GetVaultAsync(string vaultName, string resourceGroup, string subscription, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<List<BackupVaultInfo>> ListVaultsAsync(string subscription, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<ProtectResult> ProtectItemAsync(string vaultName, string resourceGroup, string subscription, string datasourceId, string policyName, string? containerName, string? datasourceType, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<ProtectedItemInfo> GetProtectedItemAsync(string vaultName, string resourceGroup, string subscription, string protectedItemName, string? containerName, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<List<ProtectedItemInfo>> ListProtectedItemsAsync(string vaultName, string resourceGroup, string subscription, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<BackupTriggerResult> TriggerBackupAsync(string vaultName, string resourceGroup, string subscription, string protectedItemName, string? containerName, string? expiry, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<RestoreTriggerResult> TriggerRestoreAsync(string vaultName, string resourceGroup, string subscription, string protectedItemName, string recoveryPointId, string? containerName, string? targetResourceId, string? restoreLocation, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<BackupPolicyInfo> GetPolicyAsync(string vaultName, string resourceGroup, string subscription, string policyName, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<List<BackupPolicyInfo>> ListPoliciesAsync(string vaultName, string resourceGroup, string subscription, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<BackupJobInfo> GetJobAsync(string vaultName, string resourceGroup, string subscription, string jobId, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<List<BackupJobInfo>> ListJobsAsync(string vaultName, string resourceGroup, string subscription, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<RecoveryPointInfo> GetRecoveryPointAsync(string vaultName, string resourceGroup, string subscription, string protectedItemName, string recoveryPointId, string? containerName, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
    Task<List<RecoveryPointInfo>> ListRecoveryPointsAsync(string vaultName, string resourceGroup, string subscription, string protectedItemName, string? containerName, string? tenant, RetryPolicyOptions? retryPolicy, CancellationToken cancellationToken);
}
