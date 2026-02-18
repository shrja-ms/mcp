// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Models;

namespace Azure.Mcp.Tools.AzureBackup.Services;

public interface IAzureBackupService
{
    // Vault operations
    Task<VaultCreateResult> CreateVaultAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string vaultType,
        string location,
        string? sku = null,
        string? storageType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<BackupVaultInfo> GetVaultAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string? vaultType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<List<BackupVaultInfo>> ListVaultsAsync(
        string subscription,
        string? vaultType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    // Protected item operations
    Task<ProtectResult> ProtectItemAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string datasourceId,
        string policyName,
        string? vaultType = null,
        string? containerName = null,
        string? datasourceType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<ProtectedItemInfo> GetProtectedItemAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string protectedItemName,
        string? vaultType = null,
        string? containerName = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<List<ProtectedItemInfo>> ListProtectedItemsAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string? vaultType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    // Backup operations
    Task<BackupTriggerResult> TriggerBackupAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string protectedItemName,
        string? vaultType = null,
        string? containerName = null,
        string? expiry = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    // Restore operations
    Task<RestoreTriggerResult> TriggerRestoreAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string protectedItemName,
        string recoveryPointId,
        string? vaultType = null,
        string? containerName = null,
        string? targetResourceId = null,
        string? restoreLocation = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    // Policy operations
    Task<BackupPolicyInfo> GetPolicyAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string policyName,
        string? vaultType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<List<BackupPolicyInfo>> ListPoliciesAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string? vaultType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    // Job operations
    Task<BackupJobInfo> GetJobAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string jobId,
        string? vaultType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<List<BackupJobInfo>> ListJobsAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string? vaultType = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    // Recovery point operations
    Task<RecoveryPointInfo> GetRecoveryPointAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string protectedItemName,
        string recoveryPointId,
        string? vaultType = null,
        string? containerName = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<List<RecoveryPointInfo>> ListRecoveryPointsAsync(
        string vaultName,
        string resourceGroup,
        string subscription,
        string protectedItemName,
        string? vaultType = null,
        string? containerName = null,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);
}
