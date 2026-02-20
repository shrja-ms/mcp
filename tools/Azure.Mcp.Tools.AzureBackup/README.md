# Azure.Mcp.Tools.AzureBackup

Azure Backup MCP (Model Context Protocol) Tools - Manage and interact with Azure Backup services through AI agents and MCP clients.

## Overview

Azure Backup is Microsoft's cloud-native data protection service for backing up and restoring resources across Azure. This MCP tool provides comprehensive operations for managing Azure Backup vaults, policies, protected items, jobs, and recovery points, enabling AI agents to:

- Create, configure, and manage Recovery Services vaults and Backup vaults
- Define and manage backup policies with custom schedules and retention
- Protect, monitor, and restore Azure resources (VMs, SQL, SAP HANA, AKS, Disks, Blobs, File Shares)
- Run end-to-end workflows for common backup scenarios (setup, migration, compliance, ransomware recovery)
- Perform governance, security, diagnostics, and cost analysis operations

**Features:**
- 57 comprehensive backup commands across 16 categories with full MCP integration
- Dual vault support: Recovery Services vaults (RSV) and Backup vaults (DPP)
- 9 end-to-end workflow commands for complex multi-step scenarios
- Robust error handling and Azure authentication
- Production-ready with 190 unit tests (100% command coverage)
- AOT-compatible with source-generated JSON serialization

## Prerequisites

- Azure subscription with Azure Backup enabled
- Azure authentication (Azure CLI or managed identity)
- Access to target Recovery Services vaults or Backup vaults

## Authentication

The tool uses Azure authentication. Ensure you're logged in using Azure CLI:

```bash
az login
```

## MCP Client Configuration

Configure your MCP client to use Azure Backup tools:

```json
{
  "servers": {
    "azure-backup": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "path/to/Azure.Mcp.Server", "--", "server", "start", "--namespace", "azurebackup"]
    }
  }
}
```

## Available Commands

**Note:** All commands support additional global options for authentication (`--tenant`), retry policies, and vault type selection (`--vault-type`). Use `--help` with any command to see the full list of options.

### Vault Operations (5 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `vault list` | Lists all backup vaults in a subscription | `--subscription` |
| `vault get` | Gets details of a specific vault | `--subscription`, `--resource-group`, `--vault` |
| `vault create` | Creates a new backup vault | `--subscription`, `--resource-group`, `--vault`, `--location`, `--vault-type` |
| `vault update` | Updates vault settings (redundancy, encryption, identity) | `--subscription`, `--resource-group`, `--vault` |
| `vault delete` | Deletes a backup vault | `--subscription`, `--resource-group`, `--vault` |

### Policy Operations (5 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `policy list` | Lists backup policies in a vault | `--subscription`, `--resource-group`, `--vault` |
| `policy get` | Gets details of a specific policy | `--subscription`, `--resource-group`, `--vault`, `--policy` |
| `policy create` | Creates a new backup policy | `--subscription`, `--resource-group`, `--vault`, `--policy`, `--workload-type` |
| `policy update` | Updates an existing policy | `--subscription`, `--resource-group`, `--vault`, `--policy` |
| `policy delete` | Deletes a backup policy | `--subscription`, `--resource-group`, `--vault`, `--policy` |

### Protected Item Operations (8 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `protecteditem list` | Lists protected items in a vault | `--subscription`, `--resource-group`, `--vault` |
| `protecteditem get` | Gets details of a protected item | `--subscription`, `--resource-group`, `--vault`, `--protected-item` |
| `protecteditem protect` | Enables backup for a resource | `--subscription`, `--resource-group`, `--vault`, `--datasource-id`, `--policy` |
| `protecteditem modify` | Modifies protection settings | `--subscription`, `--resource-group`, `--vault`, `--protected-item` |
| `protecteditem stop` | Stops protection (retain or delete data) | `--subscription`, `--resource-group`, `--vault`, `--protected-item`, `--mode` |
| `protecteditem resume` | Resumes protection after stop | `--subscription`, `--resource-group`, `--vault`, `--protected-item`, `--policy` |
| `protecteditem undelete` | Recovers a soft-deleted item | `--subscription`, `--resource-group`, `--vault`, `--protected-item` |
| `protecteditem autoprotect` | Enables auto-protection for SQL/HANA | `--subscription`, `--vault`, `--vm-resource-id`, `--policy` |

### Backup Operations (2 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `backup trigger` | Triggers an on-demand backup | `--subscription`, `--resource-group`, `--vault`, `--protected-item` |
| `backup status` | Checks backup protection status of a resource | `--subscription`, `--datasource-id`, `--location` |

### Restore Operations (1 command)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `restore trigger` | Triggers a restore operation | `--subscription`, `--resource-group`, `--vault`, `--protected-item`, `--recovery-point` |

### Job Operations (3 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `job list` | Lists backup jobs in a vault | `--subscription`, `--resource-group`, `--vault` |
| `job get` | Gets details of a backup job | `--subscription`, `--resource-group`, `--vault`, `--job` |
| `job cancel` | Cancels a running backup job | `--subscription`, `--resource-group`, `--vault`, `--job` |

### Recovery Point Operations (3 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `recoverypoint list` | Lists recovery points for an item | `--subscription`, `--resource-group`, `--vault`, `--protected-item` |
| `recoverypoint get` | Gets details of a recovery point | `--subscription`, `--resource-group`, `--vault`, `--protected-item`, `--recovery-point` |
| `recoverypoint archive` | Moves a recovery point to archive tier | `--subscription`, `--resource-group`, `--vault`, `--protected-item`, `--recovery-point` |

### Security Operations (4 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `security rbac` | Configures RBAC role assignments for backup | `--subscription`, `--principal-id`, `--role-name`, `--scope` |
| `security mua` | Configures Multi-User Authorization | `--subscription`, `--resource-group`, `--vault`, `--resource-guard-id` |
| `security privateendpoint` | Configures private endpoint connections | `--subscription`, `--resource-group`, `--vault`, `--vnet-id`, `--subnet-id` |
| `security encryption` | Configures CMK encryption for a vault | `--subscription`, `--resource-group`, `--vault`, `--key-vault-uri`, `--key-name` |

### Monitoring Operations (2 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `monitoring configure` | Configures diagnostic settings for a vault | `--subscription`, `--resource-group`, `--vault` |
| `monitoring reports` | Retrieves backup reports from Log Analytics | `--subscription`, `--report-type`, `--log-analytics-workspace-id` |

### Governance Operations (4 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `governance findunprotected` | Finds unprotected resources in a subscription | `--subscription` |
| `governance applypolicy` | Assigns Azure Policy for backup compliance | `--subscription`, `--policy-definition-id`, `--scope` |
| `governance immutability` | Configures vault immutability settings | `--subscription`, `--resource-group`, `--vault`, `--immutability-state` |
| `governance softdelete` | Configures soft delete settings | `--subscription`, `--resource-group`, `--vault`, `--soft-delete` |

### Disaster Recovery Operations (3 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `dr enablecrr` | Enables Cross-Region Restore on a vault | `--subscription`, `--resource-group`, `--vault` |
| `dr crossregionrestore` | Triggers a cross-region restore | `--subscription`, `--resource-group`, `--vault`, `--protected-item`, `--recovery-point` |
| `dr validatereadiness` | Validates DR readiness for a vault | `--subscription`, `--resource-group`, `--vault` |

### Diagnostics Operations (3 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `diagnostics validate` | Validates backup prerequisites for a resource | `--subscription`, `--resource-group`, `--vault`, `--datasource-id`, `--workload-type` |
| `diagnostics diagnose` | Diagnoses backup failures | `--subscription`, `--resource-group`, `--vault` |
| `diagnostics healthcheck` | Runs a comprehensive vault health check | `--subscription`, `--resource-group`, `--vault` |

### Bulk Operations (3 commands)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `bulk enable` | Enables backup for multiple resources at once | `--subscription`, `--resource-group`, `--vault`, `--resource-ids`, `--policy` |
| `bulk trigger` | Triggers backup for multiple items | `--subscription`, `--resource-group`, `--vault`, `--resource-ids` |
| `bulk updatepolicy` | Bulk updates policy assignment for items | `--subscription`, `--resource-group`, `--vault`, `--source-policy-name`, `--target-policy-name` |

### Cost Operations (1 command)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `cost estimate` | Estimates backup cost for a vault | `--subscription`, `--resource-group`, `--vault` |

### IaC Operations (1 command)

| Command | Description | Key Options |
|---------|-------------|-------------|
| `iac generate` | Generates Terraform or Bicep from a vault | `--subscription`, `--resource-group`, `--vault`, `--iac-format` |

### Workflow Operations (9 commands)

End-to-end workflows that orchestrate multiple operations for common scenarios:

| Command | Description | Key Options |
|---------|-------------|-------------|
| `workflow setupvm` | Sets up VM backup end-to-end | `--subscription`, `--resource-group`, `--resource-ids`, `--location`, `--vault` |
| `workflow setupsqlhana` | Sets up SQL/HANA backup on a VM | `--subscription`, `--resource-group`, `--vm-resource-id`, `--workload-type`, `--vault` |
| `workflow setupaks` | Sets up AKS cluster backup | `--subscription`, `--resource-group`, `--target-cluster-id`, `--location`, `--vault` |
| `workflow setupdatasource` | Sets up backup for any datasource | `--subscription`, `--resource-group`, `--datasource-id`, `--workload-type`, `--vault` |
| `workflow setupdr` | Sets up disaster recovery | `--subscription`, `--resource-group`, `--resource-ids`, `--location`, `--vault` |
| `workflow securevault` | Hardens vault security posture | `--subscription`, `--resource-group`, `--vault`, `--security-level` |
| `workflow compliance` | Scans and remediates backup compliance | `--subscription`, `--resource-group`, `--vault`, `--policy` |
| `workflow migrate` | Migrates backup config between vaults | `--subscription`, `--resource-group`, `--source-vault-name` |
| `workflow ransomware` | Ransomware recovery workflow | `--subscription`, `--resource-group`, `--vault`, `--resource-ids`, `--infection-timestamp` |

## Architecture

The tool uses a strategy pattern to support both vault types:

```
IAzureBackupService (facade)
  |-- IRsvBackupOperations (Recovery Services vault operations)
  |-- IDppBackupOperations (Backup vault / Data Protection operations)
```

Commands extend one of three base classes:
- **BaseAzureBackupCommand** - Vault-scoped commands (`--subscription`, `--resource-group`, `--vault`)
- **SubscriptionCommand** - Subscription-scoped commands (`--subscription`)
- **BaseProtectedItemCommand** - Item-scoped commands (adds `--protected-item`, `--container`)

## Error Handling

The tool provides detailed error messages and proper HTTP status codes:

- **400**: Bad request - missing or invalid parameters
- **401**: Authentication failed - run `az login`
- **403**: Access denied - check RBAC permissions on the vault
- **404**: Resource not found - verify vault, policy, or item names
- **500**: Internal server error - check Azure service health

## Development and Testing

### Running Tests

```bash
# Run all unit tests
dotnet test tools/Azure.Mcp.Tools.AzureBackup/tests/Azure.Mcp.Tools.AzureBackup.UnitTests

# Run with verbose output
dotnet test tools/Azure.Mcp.Tools.AzureBackup/tests/Azure.Mcp.Tools.AzureBackup.UnitTests --verbosity normal

# Run a specific test class
dotnet test --filter "FullyQualifiedName~VaultGetCommandTests"
```

### Test Coverage

- **Total Tests**: 190 (all passing)
- **Coverage**: All 57 commands have unit tests
- **Test Pattern**: Each command has 3 tests (success, handles not found, handles exception)

### Building

```bash
# Build the tool project
dotnet build tools/Azure.Mcp.Tools.AzureBackup/src

# Build the test project
dotnet build tools/Azure.Mcp.Tools.AzureBackup/tests/Azure.Mcp.Tools.AzureBackup.UnitTests
```

## Contributing

This tool is part of the Microsoft MCP (Model Context Protocol) project. Please follow the established patterns for command implementation and ensure proper error handling and logging.

### Development Guidelines

- Follow the `{Resource}{Operation}Command` naming pattern
- Use primary constructors and sealed classes
- Register all commands in `AzureBackupSetup.cs`
- Register all response models in `AzureBackupJsonContext` for AOT safety
- Use `AzureBackupOptionDefinitions` constants for option names
- Always call `base.RegisterOptions()` in overrides
- Use `HandleException(context, ex)` in catch blocks
- Write tests that verify success, not-found, and exception scenarios

## Support and Documentation

- [Azure Backup Documentation](https://docs.microsoft.com/azure/backup/)
- [Recovery Services Vault Documentation](https://docs.microsoft.com/azure/backup/backup-azure-recovery-services-vault-overview)
- [Azure CLI Authentication](https://docs.microsoft.com/cli/azure/authenticate-azure-cli)
