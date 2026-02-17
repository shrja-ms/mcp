// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.DataProtection.Models;

namespace Azure.Mcp.Tools.DataProtection.Services;

public interface IDataProtectionService
{
    Task<IEnumerable<BackupVaultModel>> ListVaultsAsync(
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<BackupVaultModel> GetVaultAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BackupInstanceModel>> ListBackupInstancesAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<BackupInstanceModel> GetBackupInstanceAsync(
        string backupInstance,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BackupPolicyModel>> ListPoliciesAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<BackupPolicyModel> GetPolicyAsync(
        string policy,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<BackupJobModel>> ListJobsAsync(
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<BackupJobModel> GetJobAsync(
        string job,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<RecoveryPointModel>> ListRecoveryPointsAsync(
        string backupInstance,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);

    Task<RecoveryPointModel> GetRecoveryPointAsync(
        string recoveryPoint,
        string backupInstance,
        string vault,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        RetryPolicyOptions? retryPolicy = null,
        CancellationToken cancellationToken = default);
}
