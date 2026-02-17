// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.DataProtection.Models;

public class BackupVaultModel
{
    public string? Name { get; set; }
    public string? ResourceGroup { get; set; }
    public string? Location { get; set; }
    public string? ProvisioningState { get; set; }
    public string? StorageType { get; set; }
    public string? CrossRegionRestoreState { get; set; }
    public string? SoftDeleteState { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
}

public class BackupInstanceModel
{
    public string? Name { get; set; }
    public string? DataSourceType { get; set; }
    public string? DataSourceId { get; set; }
    public string? PolicyName { get; set; }
    public string? ProtectionStatus { get; set; }
    public string? CurrentProtectionState { get; set; }
    public string? ProvisioningState { get; set; }
}

public class BackupPolicyModel
{
    public string? Name { get; set; }
    public string? DataSourceType { get; set; }
    public IList<string>? DataStoreTypes { get; set; }
}

public class BackupJobModel
{
    public string? Name { get; set; }
    public string? Operation { get; set; }
    public string? Status { get; set; }
    public string? DataSourceName { get; set; }
    public string? DataSourceType { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? Duration { get; set; }
    public bool? IsUserTriggered { get; set; }
}

public class RecoveryPointModel
{
    public string? Name { get; set; }
    public string? RecoveryPointType { get; set; }
    public DateTimeOffset? RecoveryPointTime { get; set; }
}
