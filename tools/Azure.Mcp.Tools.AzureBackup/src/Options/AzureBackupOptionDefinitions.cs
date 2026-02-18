// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.AzureBackup.Options;

public static class AzureBackupOptionDefinitions
{
    public const string VaultName = "vault";
    public const string VaultTypeName = "vault-type";
    public const string ProtectedItemName = "protected-item";
    public const string ContainerName = "container";
    public const string PolicyName = "policy";
    public const string JobName = "job";
    public const string RecoveryPointName = "recovery-point";
    public const string LocationName = "location";
    public const string DatasourceIdName = "datasource-id";
    public const string DatasourceTypeName = "datasource-type";
    public const string SkuName = "sku";
    public const string StorageTypeName = "storage-type";
    public const string ExpiryName = "expiry";
    public const string TargetResourceIdName = "target-resource-id";
    public const string RestoreLocationName = "restore-location";

    public static readonly Option<string> Vault = new($"--{VaultName}")
    {
        Description = "The name of the backup vault (Recovery Services vault or Backup vault).",
        Required = true
    };

    public static readonly Option<string> VaultType = new($"--{VaultTypeName}")
    {
        Description = "The type of backup vault: 'rsv' (Recovery Services vault) or 'dpp' (Backup vault / Data Protection). Required for vault create; optional elsewhere (auto-detected if omitted).",
        Required = false
    };

    public static readonly Option<string> ProtectedItem = new($"--{ProtectedItemName}")
    {
        Description = "The name of the protected item or backup instance. For RSV, this is the protected item name. For DPP, this is the backup instance name.",
        Required = true
    };

    public static readonly Option<string> Container = new($"--{ContainerName}")
    {
        Description = "The RSV protection container name. Only applicable for Recovery Services vaults. If omitted, will be auto-derived when possible.",
        Required = false
    };

    public static readonly Option<string> Policy = new($"--{PolicyName}")
    {
        Description = "The name of the backup policy.",
        Required = true
    };

    public static readonly Option<string> Job = new($"--{JobName}")
    {
        Description = "The backup job ID.",
        Required = true
    };

    public static readonly Option<string> RecoveryPoint = new($"--{RecoveryPointName}")
    {
        Description = "The recovery point ID.",
        Required = true
    };

    public static readonly Option<string> Location = new($"--{LocationName}")
    {
        Description = "The Azure region where the vault will be created (e.g., 'eastus', 'westus2').",
        Required = true
    };

    public static readonly Option<string> DatasourceId = new($"--{DatasourceIdName}")
    {
        Description = "The ARM resource ID of the datasource to protect (e.g., the VM, disk, or database resource ID).",
        Required = true
    };

    public static readonly Option<string> DatasourceType = new($"--{DatasourceTypeName}")
    {
        Description = "The workload type hint for the datasource (e.g., 'AzureVM', 'AzureDisk', 'AzureDatabase'). Optional; helps with routing.",
        Required = false
    };

    public static readonly Option<string> Sku = new($"--{SkuName}")
    {
        Description = "The vault SKU. For RSV: 'Standard'. For DPP: 'Standard' or other supported SKUs.",
        Required = false
    };

    public static readonly Option<string> StorageType = new($"--{StorageTypeName}")
    {
        Description = "The storage redundancy type: 'GeoRedundant', 'LocallyRedundant', or 'ZoneRedundant'.",
        Required = false
    };

    public static readonly Option<string> Expiry = new($"--{ExpiryName}")
    {
        Description = "The recovery point expiry time in ISO 8601 format (e.g., '2025-12-31T23:59:59Z'). Optional for backup trigger.",
        Required = false
    };

    public static readonly Option<string> TargetResourceId = new($"--{TargetResourceIdName}")
    {
        Description = "The ARM resource ID of the target resource for alternate-location restore.",
        Required = false
    };

    public static readonly Option<string> RestoreLocation = new($"--{RestoreLocationName}")
    {
        Description = "The Azure region to restore to. If omitted, restores to the original location.",
        Required = false
    };
}
