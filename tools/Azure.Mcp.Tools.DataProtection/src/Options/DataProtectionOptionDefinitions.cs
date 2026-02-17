// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.DataProtection.Options;

public static class DataProtectionOptionDefinitions
{
    public const string VaultName = "vault";
    public const string BackupInstanceName = "backup-instance";
    public const string PolicyName = "policy";
    public const string JobName = "job";
    public const string RecoveryPointName = "recovery-point";

    public static readonly Option<string> Vault = new($"--{VaultName}")
    {
        Description = "The name of the Azure Backup vault.",
        Required = true
    };

    public static readonly Option<string> BackupInstance = new($"--{BackupInstanceName}")
    {
        Description = "The name of the backup instance within the vault.",
        Required = true
    };

    public static readonly Option<string> Policy = new($"--{PolicyName}")
    {
        Description = "The name of the backup policy within the vault.",
        Required = true
    };

    public static readonly Option<string> Job = new($"--{JobName}")
    {
        Description = "The ID of the backup job within the vault.",
        Required = true
    };

    public static readonly Option<string> RecoveryPoint = new($"--{RecoveryPointName}")
    {
        Description = "The ID of the recovery point for a backup instance.",
        Required = true
    };
}
