# Azure Backup MCP — Developer Guidance

This document describes the work required when adding a new backup workload type or integrating a new SDK feature into the Azure Backup MCP toolset.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Adding a New DPP Workload](#adding-a-new-dpp-workload)
3. [Adding a New RSV Workload](#adding-a-new-rsv-workload)
4. [Policy Creation Patterns](#policy-creation-patterns)
5. [Protection Patterns](#protection-patterns)
6. [Backup Trigger Patterns](#backup-trigger-patterns)
7. [Restore Patterns](#restore-patterns)
8. [Adding a New SDK Feature or Parameter](#adding-a-new-sdk-feature-or-parameter)
9. [AOT and JSON Serialization](#aot-and-json-serialization)
10. [Testing Guidance](#testing-guidance)
11. [Common Pitfalls](#common-pitfalls)

---

## Architecture Overview

The Azure Backup MCP uses a **two-tier vault architecture**:

| Tier | Vault Type | SDK Package | Workloads |
|------|-----------|-------------|-----------|
| **RSV** | Recovery Services Vault | `Azure.ResourceManager.RecoveryServicesBackup` | VM, SQL, SAP HANA, File Shares |
| **DPP** | Backup Vault | `Azure.ResourceManager.DataProtectionBackup` | Disk, Blob, PostgreSQL Flex, MySQL Flex, AKS, Elastic SAN |

### Service Layer Routing

```
Command (e.g., PolicyCreateCommand)
  └─► AzureBackupService        (unified facade)
        ├─► VaultTypeResolver   (IsRsv / IsDpp routing)
        ├─► RsvBackupOperations  (Recovery Services SDK calls)
        └─► DppBackupOperations  (Data Protection SDK calls)
```

**Key files:**

| File | Purpose |
|------|---------|
| `src/Services/AzureBackupService.cs` | Unified facade — routes every operation to RSV or DPP |
| `src/Services/VaultTypeResolver.cs` | `IsRsv()` / `IsDpp()` helpers and auto-detect logic |
| `src/Services/RsvBackupOperations.cs` | All RSV SDK operations |
| `src/Services/DppBackupOperations.cs` | All DPP SDK operations |
| `src/Services/IRsvBackupOperations.cs` | RSV interface |
| `src/Services/IDppBackupOperations.cs` | DPP interface |
| `src/Commands/AzureBackupJsonContext.cs` | AOT JSON serialization context |
| `src/AzureBackupSetup.cs` | DI registration and command group wiring |

---

## Adding a New DPP Workload

When a new workload (e.g., Elastic SAN, MySQL Flex, Cosmos DB) is added to the Azure Backup DPP platform, the following code locations **must** be updated in `DppBackupOperations.cs`:

### Checklist

#### 1. Add to `MapWorkloadTypeToArmResourceType()` (line ~24)

Maps the friendly workload name to the ARM resource type string.

```csharp
private static string MapWorkloadTypeToArmResourceType(string workloadType) => workloadType.ToLowerInvariant() switch
{
    "azuredisk" => "Microsoft.Compute/disks",
    "azureblob" => "Microsoft.Storage/storageAccounts/blobServices",
    "postgresqlflexible" => "Microsoft.DBforPostgreSQL/flexibleServers",
    "mysqlflexible" => "Microsoft.DBforMySQL/flexibleServers",
    "aks" => "Microsoft.ContainerService/managedClusters",
    "elasticsan" => "Microsoft.ElasticSan/elasticSans/volumeGroups",
    // ✅ Add your new workload here:
    "newworkload" => "Microsoft.Provider/resourceType",
    _ => workloadType
};
```

#### 2. Add to `UsesOperationalStore()` (line ~38)

Determines whether the workload uses OperationalStore (snapshot-based) or VaultStore.

```csharp
private static bool UsesOperationalStore(string datasourceType) => datasourceType.ToLowerInvariant() switch
{
    "microsoft.compute/disks" => true,                        // Disk → snapshot
    "microsoft.storage/storageaccounts/blobservices" => true, // Blob → continuous
    "microsoft.containerservice/managedclusters" => true,     // AKS → snapshot
    "microsoft.elasticsan/elasticsans/volumegroups" => true,  // ESAN → snapshot
    // ✅ Add both ARM type AND friendly name entries if OperationalStore:
    "newworkload" => true,
    "microsoft.provider/resourcetype" => true,
    _ => false  // Default: VaultStore (PGFlex, MySQL Flex)
};
```

> **Rule of thumb:** If the workload creates snapshots (Disk, AKS, ESAN) or continuous backups (Blob), it uses OperationalStore. If it writes full/incremental backups to vault storage (PGFlex, MySQL Flex), it uses VaultStore.

#### 3. Evaluate special-case boolean helpers

Determine if the workload needs a dedicated boolean helper similar to existing ones:

| Helper | When Needed | Example |
|--------|-------------|---------|
| `IsBlobOperationalBackup()` | Continuous backup, no scheduled backup rule | Azure Blob |
| `IsElasticSanWorkload()` | Parent-child resource hierarchy requiring `DataSourceSetInfo` | Elastic SAN |

If the new workload has a **parent-child resource hierarchy** (e.g., the datasource is a child resource and backup requires a reference to the parent), create a helper like `IsElasticSanWorkload()` and a corresponding `Get[Workload]ParentId()` method.

If the new workload uses **continuous backup** (no schedule), add it to `IsBlobOperationalBackup()` or create a similar helper.

#### 4. Update `CreatePolicyAsync()` — Schedule and Backup Configuration (line ~690)

The policy creation logic branches by workload characteristics:

```
Blob:          continuous (no backup rule)
Disk/AKS:      PT4H schedule, Incremental, OperationalStore
ESAN:          P1D schedule, Incremental, OperationalStore  
PGFlex/MySQL:  P1D schedule, Full, VaultStore
```

Determine which pattern your workload matches, or add a new branch:

```csharp
// In CreatePolicyAsync:
var isElasticSan = IsElasticSanWorkload(workloadType);
var useHourlySchedule = useOperationalStore && !isElasticSan;
// ✅ If your workload uses OperationalStore with daily (not hourly) schedule:
// var useHourlySchedule = useOperationalStore && !isElasticSan && !IsNewWorkload(workloadType);
```

Also verify the `BackupParameters` setting:
- OperationalStore workloads typically use `"Incremental"`
- VaultStore workloads typically use `"Full"`

#### 5. Update `ProtectItemAsync()` — DataSourceInfo and DataSourceSetInfo (line ~170)

Standard protection only requires `DataSourceInfo`. Parent-child workloads also need `DataSourceSetInfo`:

```csharp
// Standard workload (no parent-child):
var dataSourceInfo = new DataSourceInfo(datasourceResourceId) { ... };

// Parent-child workload (like ESAN):
if (IsElasticSanWorkload(resolvedDatasourceType))
{
    var parentId = GetElasticSanParentId(datasourceResourceId);
    instanceProperties.DataSourceSetInfo = new DataSourceSetInfo(parentId) { ... };
}
```

Also check:
- **Instance naming**: ESAN uses `{parentName}-{childName}-{guid}`, others use `{name}-{name}-{guid12}`
- **Snapshot resource group**: If `UsesOperationalStore()` and not `IsBlobOperationalBackup()`, the code sets `PolicyParameters` with `OperationalDataStoreSettings`

#### 6. Update `TriggerBackupAsync()` — Rule Name (line ~310)

The backup trigger looks up the policy's backup rule name. Most workloads work with auto-detection from the policy. No changes needed unless the new workload uses a non-standard rule naming convention.

#### 7. Update `TriggerRestoreAsync()` — Restore Target Construction (line ~360)

Restore has three paths:
1. **Restore-as-files** — target is a storage account (PGFlex, MySQL Flex)
2. **Restore-as-server** — target is the original or alternate datasource (Disk, AKS, PGFlex ALR)
3. **Point-in-time** — for continuous backup workloads (Blob)

If the workload uses parent-child resources:
```csharp
if (IsElasticSanWorkload(resolvedDatasourceTypeStr))
{
    restoreTargetInfo.DataSourceSetInfo = new DataSourceSetInfo(parentId) { ... };
}
```

Also verify `SourceDataStoreType`:
- `SourceDataStoreType.OperationalStore` for snapshot-based workloads
- `SourceDataStoreType.VaultStore` for vault-stored workloads

### Summary: Files to Modify for a New DPP Workload

| File | Changes |
|------|---------|
| `DppBackupOperations.cs` | Add mappings in 4-7 functions (see checklist above) |
| `AzureBackupJsonContext.cs` | Register any new model types (if adding new response models) |
| `README.md` | Update supported workloads list |

---

## Adding a New RSV Workload

RSV workloads are more complex due to container-based naming and workload-specific SDK types.

### Checklist

#### 1. Update `IsWorkloadType()` helper

RSV uses this to distinguish VMs from database workloads (SQL/HANA):

```csharp
private static bool IsWorkloadType(string? datasourceType) =>
    datasourceType?.ToUpperInvariant() is "SQLDATABASE" or "MSSQL" or "SAPHANA" or "SAPHANADATABASE"
    // ✅ Add: or "NEWWORKLOADTYPE"
    ;
```

#### 2. Update `ProtectItemAsync()` — Protected Item Type Selection

RSV uses different SDK types per workload:

```csharp
BackupGenericProtectedItem protectedItemProperties = datasourceType?.ToUpperInvariant() switch
{
    "SQLDATABASE" or "MSSQL" => new VmWorkloadSqlDatabaseProtectedItem { PolicyId = policyArmId },
    "SAPHANA" or "SAPHANADATABASE" => new VmWorkloadSapHanaDatabaseProtectedItem { PolicyId = policyArmId },
    // ✅ Add: "NEWTYPE" => new VmWorkloadNewTypeProtectedItem { PolicyId = policyArmId },
    _ => new VmWorkloadSqlDatabaseProtectedItem { PolicyId = policyArmId }
};
```

#### 3. Update `CreatePolicyAsync()` — Policy Type Selection

RSV has two policy patterns:

| Pattern | Workloads | SDK Type |
|---------|-----------|----------|
| Standard VM | Azure VM | `IaasVmProtectionPolicy` with `SimpleSchedulePolicy` |
| Workload | SQL, SAP HANA | `VmWorkloadProtectionPolicy` with `SubProtectionPolicy` (Full + Log) |

If the new workload is a database workload, update the `VmWorkloadProtectionPolicy` section. If it's a different type, you may need a new policy branch.

```csharp
if (IsWorkloadType(workloadType))
{
    var wlPolicy = new VmWorkloadProtectionPolicy
    {
        WorkLoadType = new BackupWorkloadType(workloadType.ToUpperInvariant() switch
        {
            "SQLDATABASE" or "MSSQL" => "SQLDataBase",
            "SAPHANA" or "SAPHANADATABASE" => "SAPHanaDatabase",
            // ✅ Add: "NEWTYPE" => "NewTypeName",
            _ => "SQLDataBase"
        }),
        Settings = new BackupCommonSettings { TimeZone = "UTC", IsCompression = false, IsSqlCompression = false }
    };
    // Add Full + Log sub-policies...
}
```

#### 4. Update `TriggerBackupAsync()` — Backup Content Type

RSV auto-detects workload type from naming patterns for backup triggers:

```csharp
private static BackupContent CreateBackupRequestContent(...)
{
    var isWorkload = protectedItemName.StartsWith("sqldatabase;", ...) ||
                     protectedItemName.StartsWith("saphanadatabase;", ...);
    // ✅ Add: || protectedItemName.StartsWith("newtype;", ...);
}
```

#### 5. Update `TriggerRestoreAsync()` — Restore Content Type

RSV switches on the existing protected item type:

```csharp
if (existingProperties is VmWorkloadSqlDatabaseProtectedItem)
    restoreProperties = CreateWorkloadSqlRestoreContent(...);
else if (existingProperties is VmWorkloadSapHanaDatabaseProtectedItem)
    restoreProperties = CreateWorkloadSapHanaRestoreContent(...);
// ✅ Add: else if (existingProperties is VmWorkloadNewTypeProtectedItem)
//     restoreProperties = CreateWorkloadNewTypeRestoreContent(...);
else
    // Standard VM restore...
```

#### 6. Update `StopProtectionAsync()` and `ResumeProtectionAsync()`

These methods switch on item type to create the correct stop/resume payload:

```csharp
BackupGenericProtectedItem stopProps = existingItem.Value.Data.Properties switch
{
    VmWorkloadSqlDatabaseProtectedItem => new VmWorkloadSqlDatabaseProtectedItem { ... },
    VmWorkloadSapHanaDatabaseProtectedItem => new VmWorkloadSapHanaDatabaseProtectedItem { ... },
    // ✅ Add type-specific handling
    _ => new IaasComputeVmProtectedItem { ... }
};
```

### Summary: Files to Modify for a New RSV Workload

| File | Changes |
|------|---------|
| `RsvBackupOperations.cs` | Update 5-7 functions (see checklist above) |
| `RsvNamingHelper.cs` | Add container/item name derivation rules (if applicable) |
| `AzureBackupJsonContext.cs` | Register any new SDK model types |
| `README.md` | Update supported workloads list |

---

## Policy Creation Patterns

### DPP Policy Matrix

| Workload | Schedule | Backup Type | Data Store | Retention Default |
|----------|----------|-------------|------------|-------------------|
| Azure Disk | PT4H (hourly) | Incremental | OperationalStore | 7 days |
| AKS | PT4H (hourly) | Incremental | OperationalStore | 7 days |
| Azure Blob | Continuous (no rule) | N/A | OperationalStore | 7 days |
| PostgreSQL Flex | P1D (daily) | Full | VaultStore | 30 days |
| MySQL Flex | P1D (daily) | Full | VaultStore | 30 days |
| Elastic SAN | P1D (daily) | Incremental | OperationalStore | 7 days |

### RSV Policy Matrix

| Workload | Policy Type | Schedule | Sub-Policies |
|----------|-------------|----------|-------------|
| Azure VM | `IaasVmProtectionPolicy` | Daily `SimpleSchedulePolicy` | N/A (single schedule + retention) |
| SQL Database | `VmWorkloadProtectionPolicy` | Sub-policies | Full (daily) + Log (hourly, 60 min) |
| SAP HANA | `VmWorkloadProtectionPolicy` | Sub-policies | Full (daily) + Log (hourly, 60 min) |

### Decision Tree for New DPP Workload Policy

```
Is it continuous backup (like Blob)?
  ├─► YES: No backup rule needed, only retention rule
  └─► NO: Does it use OperationalStore (snapshot)?
        ├─► YES: Does it need hourly schedule (like Disk/AKS)?
        │     ├─► YES: PT4H, Incremental, OperationalStore
        │     └─► NO:  P1D, Incremental, OperationalStore (like ESAN)
        └─► NO: P1D, Full, VaultStore (like PGFlex)
```

---

## Protection Patterns

### DPP Protection Variants

| Variant | Workloads | Key Difference |
|---------|-----------|---------------|
| **Standard** | Disk, PGFlex, MySQL Flex, AKS | `DataSourceInfo` only |
| **Auto-detect Blob** | Blob | `storageAccounts` → `storageAccounts/blobServices` type mapping |
| **Parent-child** | Elastic SAN | Requires `DataSourceSetInfo` pointing to parent resource |
| **Snapshot RG** | Disk, AKS, ESAN | Sets `PolicyParameters` with `OperationalDataStoreSettings` |

### RSV Protection Variants

| Variant | Workloads | Key Difference |
|---------|-----------|---------------|
| **VM** | Azure VM | Container discovery + `IaasComputeVmProtectedItem` |
| **SQL/HANA** | SQL, SAP HANA | Container required, uses `VmWorkloadSql/HanaProtectedItem` |

---

## Backup Trigger Patterns

### DPP

All DPP workloads use the same trigger flow:
1. Fetch the backup instance → get policy ID
2. Fetch policy → find the backup rule name (`BackupDaily`, `BackupHourly`)
3. Create `AdhocBackupTriggerContent` with the rule name

> **Exception:** Blob backup is continuous — on-demand trigger is not applicable.

### RSV

RSV backup trigger auto-detects from naming conventions:
- VM items → `IaasVmBackupContent`
- SQL/HANA items (name starts with `sqldatabase;`, `saphanadatabase;`, etc.) → `WorkloadBackupContent` with `BackupType = "Full"`

---

## Restore Patterns

### DPP Restore Matrix

| Scenario | Workloads | Target | Restore Class |
|----------|-----------|--------|---------------|
| **OLR** (Original Location) | Disk, AKS, ESAN | Same datasource | `RestoreTargetInfo` + `BackupRecoveryPointBasedRestoreContent` |
| **ALR** (Alternate Location) | Disk, AKS, PGFlex | Different datasource | `RestoreTargetInfo` with target datasource |
| **Restore-as-files** | PGFlex, MySQL Flex | Storage account | `RestoreFilesTargetInfo` + `RestoreFilesTargetDetails` |
| **Point-in-time** | Blob | N/A | `BackupRecoveryTimeBasedRestoreContent` |
| **Parent-child OLR** | ESAN | Same datasource | `RestoreTargetInfo` + `DataSourceSetInfo` |

### RSV Restore Matrix

| Scenario | Workloads | Key Parameters |
|----------|-----------|---------------|
| **VM OLR** | VM | `RecoveryType = OriginalLocation` |
| **VM RestoreDisks** | VM | `RecoveryType = RestoreDisks`, `TargetResourceGroupId` |
| **VM ALR** | VM | `RecoveryType = AlternateLocation`, target VM name, VNet, subnet |
| **SQL OLR** | SQL | `RestoreOverwriteOption.Overwrite`, same instance |
| **SQL ALR** | SQL | `RestoreOverwriteOption.FailOnConflict`, target DB + instance + data directory mappings |
| **HANA OLR** | SAP HANA | Similar to SQL OLR |
| **HANA ALR** | SAP HANA | Similar to SQL ALR |

### Key Considerations for New Restore Scenarios

1. **SourceDataStoreType** must match the data store used in the policy:
   - OperationalStore workloads → `SourceDataStoreType.OperationalStore`
   - VaultStore workloads → `SourceDataStoreType.VaultStore`

2. **DataSourceSetInfo** is required for parent-child workloads (ESAN) — both for protection and restore

3. **Recovery point vs point-in-time**: Continuous workloads (Blob) use `BackupRecoveryTimeBasedRestoreContent`; discrete workloads use `BackupRecoveryPointBasedRestoreContent`

---

## Adding a New SDK Feature or Parameter

When a new optional parameter or feature is added to the Azure Backup SDK, follow this flow:

### 1. Determine Scope

Ask: Does this feature apply to a specific operation (e.g., restore) or is it cross-cutting (e.g., a new authentication method)?

### 2. Add the Option to `AzureBackupOptionDefinitions.cs`

```csharp
// In AzureBackupOptionDefinitions.cs
public static readonly OptionDefinition NewParameter = new("new-parameter", "Description of the new parameter");
```

### 3. Add to the Options Class

```csharp
// In the relevant Options class (e.g., RestoreOptions.cs)
public string? NewParameter { get; set; }
```

### 4. Register in the Command

```csharp
protected override void RegisterOptions(Command command)
{
    base.RegisterOptions(command);
    command.Options.Add(AzureBackupOptionDefinitions.NewParameter.AsOptional());
}
```

### 5. Bind in the Command

```csharp
protected override RestoreOptions BindOptions(ParseResult parseResult)
{
    var options = base.BindOptions(parseResult);
    options.NewParameter = parseResult.GetValueOrDefault<string>(AzureBackupOptionDefinitions.NewParameter.Name);
    return options;
}
```

### 6. Thread Through Service Layers

```
Command → AzureBackupService → RsvBackupOperations / DppBackupOperations
```

Each layer must accept and forward the parameter:
- `IAzureBackupService` interface
- `AzureBackupService` implementation
- `IRsvBackupOperations` / `IDppBackupOperations` interface
- `RsvBackupOperations` / `DppBackupOperations` implementation

### 7. Update Tests

- Add unit tests in the corresponding `*CommandTests.cs` file
- Add live tests if the feature requires Azure resource interaction

### Checklist for SDK Feature Integration

| Step | File(s) |
|------|---------|
| Define option | `src/Options/AzureBackupOptionDefinitions.cs` |
| Add to options class | `src/Options/{Operation}Options.cs` |
| Register + bind in command | `src/Commands/{Category}/{Operation}Command.cs` |
| Update service interface | `src/Services/IAzureBackupService.cs` |
| Update service facade | `src/Services/AzureBackupService.cs` |
| Update RSV interface | `src/Services/IRsvBackupOperations.cs` (if RSV-applicable) |
| Update RSV implementation | `src/Services/RsvBackupOperations.cs` (if RSV-applicable) |
| Update DPP interface | `src/Services/IDppBackupOperations.cs` (if DPP-applicable) |
| Update DPP implementation | `src/Services/DppBackupOperations.cs` (if DPP-applicable) |
| AOT registration | `src/Commands/AzureBackupJsonContext.cs` (if new models) |
| Unit tests | `tests/*UnitTests/` |

---

## AOT and JSON Serialization

All response model types must be registered in `AzureBackupJsonContext.cs` for AOT compatibility:

```csharp
[JsonSerializable(typeof(VaultCreateResult))]
[JsonSerializable(typeof(BackupVaultInfo))]
[JsonSerializable(typeof(ProtectResult))]
// ✅ Add any new model type here
[JsonSerializable(typeof(NewFeatureResult))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AzureBackupJsonContext : JsonSerializerContext;
```

### Rules

- **Always use `System.Text.Json`** — never Newtonsoft
- **All public model properties** must be serializable by the source generator
- **Avoid polymorphic serialization** unless you register all derived types
- **Test AOT compilation** with `./eng/scripts/Build-Local.ps1 -BuildNative`

---

## Testing Guidance

### Unit Tests (Required)

Every command must have unit tests covering:

```csharp
[Fact] public void Constructor_InitializesCommandCorrectly()
[Fact] public async Task ExecuteAsync_CallsServiceWithCorrectParameters()
[Fact] public async Task ExecuteAsync_HandlesServiceErrors()
[Fact] public void BindOptions_BindsOptionsCorrectly()
```

### Live Tests (Required for Azure service changes)

Test the full backup lifecycle for any new workload:

| Test | Operation |
|------|-----------|
| 1 | `vault create` (or use existing) |
| 2 | `policy create` with the new workload type |
| 3 | `policy get` — verify schedule and retention |
| 4 | `protecteditem protect` — enable backup |
| 5 | `protecteditem list` — verify protected item appears |
| 6 | `backup trigger` — trigger on-demand backup |
| 7 | `job get` / `job list` — monitor backup job |
| 8 | `recoverypoint list` — verify recovery points exist |
| 9 | `restore trigger` — test OLR (and ALR if applicable) |
| 10 | `protecteditem stop` — stop protection (RetainData) |
| 11 | `protecteditem stop` — stop protection (DeleteData) |
| 12 | `policy delete` — cleanup |

### Infrastructure

- Add workload-specific test resources to `tests/test-resources.bicep`
- Update `tests/test-resources-post.ps1` for post-deployment setup
- Deploy with: `./eng/scripts/Deploy-TestResources.ps1 -Paths AzureBackup`

---

## Common Pitfalls

### 1. Forgetting Both Friendly Name AND ARM Type in Switch Statements

Most mapping functions accept both `"elasticsan"` (friendly) and `"Microsoft.ElasticSan/elasticSans/volumeGroups"` (ARM type). Always add **both** entries:

```csharp
"microsoft.elasticsan/elasticsans/volumegroups" => true,
"elasticsan" => true,
```

### 2. OperationalStore Without Snapshot RG

If `UsesOperationalStore()` returns `true` and `IsBlobOperationalBackup()` returns `false`, the protection flow sets `PolicyParameters` with `OperationalDataStoreSettings` for the snapshot resource group. Missing this causes a 400 error from the API.

### 3. DataSourceSetInfo For Parent-Child Resources

Workloads where the protected item is a child resource (e.g., ESAN volume groups are children of Elastic SANs) must set `DataSourceSetInfo` pointing to the **parent** resource in **both** `ProtectItemAsync()` and `TriggerRestoreAsync()`. Forgetting either location causes API errors.

### 4. Wrong SourceDataStoreType in Restore

OperationalStore workloads must use `SourceDataStoreType.OperationalStore` in restore requests. Using `VaultStore` for a snapshot-based workload results in "no recovery points found" errors.

### 5. SDK TimeSpan Parsing Bugs

Some Azure SDK versions have bugs parsing `TimeSpan` values like `PT0S` or `PT6M37.2384913S` in job responses. If `job_get` fails with deserialization errors, this is likely an SDK bug — document it and check for SDK updates.

### 6. RSV Container Discovery Timing

VM protection in RSV requires container discovery (`RefreshProtectionContainerAsync`) with a delay before the VM appears. The current implementation uses a 30-second delay. If protection fails with "container not found", the discovery may not have completed.

### 7. RSV Job ID vs Operation ID

RSV's `Azure-AsyncOperation` header returns an **operation ID**, not a job ID. The code uses `FindLatestJobIdAsync()` to retrieve the actual job ID by listing recent jobs. New operations should follow this same pattern.

---

## Quick Reference: Where to Add Code by Operation

| When adding a new... | DPP File | RSV File | Shared Files |
|----------------------|----------|----------|--------------|
| Workload mapping | `DppBackupOperations.cs` | `RsvBackupOperations.cs` | — |
| Policy pattern | `DppBackupOperations.cs` `CreatePolicyAsync()` | `RsvBackupOperations.cs` `CreatePolicyAsync()` | — |
| Protection pattern | `DppBackupOperations.cs` `ProtectItemAsync()` | `RsvBackupOperations.cs` `ProtectItemAsync()` | — |
| Restore pattern | `DppBackupOperations.cs` `TriggerRestoreAsync()` | `RsvBackupOperations.cs` `TriggerRestoreAsync()` | — |
| CLI parameter | — | — | `AzureBackupOptionDefinitions.cs`, `{Operation}Options.cs`, `{Operation}Command.cs` |
| Service interface method | — | — | `IAzureBackupService.cs`, `AzureBackupService.cs`, `I{Rsv/Dpp}BackupOperations.cs` |
| Response model | — | — | `Models/`, `AzureBackupJsonContext.cs` |
| Vault type routing | — | — | `VaultTypeResolver.cs`, `AzureBackupService.cs` |
