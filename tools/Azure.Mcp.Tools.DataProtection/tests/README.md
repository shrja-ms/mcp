# Azure Data Protection MCP Tools - Testing Guide

This document describes how to configure and run tests, perform live validation, and share the Azure Data Protection MCP tools with peers.

## Overview

The Data Protection tools provide 10 read-only MCP commands across 5 resource types:

| Resource Type | Commands | Description |
|---|---|---|
| **Vault** | `list`, `get` | List and retrieve Azure Backup vaults |
| **Backup Instance** | `list`, `get` | List and retrieve backup instances in a vault |
| **Policy** | `list`, `get` | List and retrieve backup policies in a vault |
| **Job** | `list`, `get` | List and retrieve backup jobs in a vault |
| **Recovery Point** | `list`, `get` | List and retrieve recovery points for a backup instance |

All commands are read-only, non-destructive, and idempotent.

## Prerequisites

### For Unit Tests

- .NET 10 SDK

### For Live Testing

- .NET 10 SDK
- Azure CLI (`az login` authenticated)
- An Azure subscription with:
  - At least one Azure Backup vault
  - At least one backup instance configured
  - A backup policy defined
- Node.js and npm (for MCP Inspector)

## Unit Tests

Unit tests cover command logic, argument parsing, option binding, result serialization, and error handling using mocked services (NSubstitute).

### Test Coverage

The unit test suite includes 10 test classes with 59 tests:

- **VaultListCommandTests** - Vault listing: success, empty results, error handling, missing parameters
- **VaultGetCommandTests** - Vault retrieval: success, error handling, missing parameters (subscription, resource-group, vault)
- **BackupInstanceListCommandTests** - Backup instance listing: success, empty results, error handling, missing parameters
- **BackupInstanceGetCommandTests** - Backup instance retrieval: success, error handling, missing parameters
- **PolicyListCommandTests** - Policy listing: success, empty results, error handling, missing parameters
- **PolicyGetCommandTests** - Policy retrieval: success, error handling, missing parameters
- **JobListCommandTests** - Job listing: success, empty results, error handling, missing parameters
- **JobGetCommandTests** - Job retrieval: success, error handling, missing parameters
- **RecoveryPointListCommandTests** - Recovery point listing: success, empty results, error handling, missing parameters
- **RecoveryPointGetCommandTests** - Recovery point retrieval: success, error handling, missing parameters

### Running Unit Tests

```bash
# Run all DataProtection unit tests
dotnet test tools/Azure.Mcp.Tools.DataProtection/tests/Azure.Mcp.Tools.DataProtection.UnitTests

# Run specific test class
dotnet test tools/Azure.Mcp.Tools.DataProtection/tests/Azure.Mcp.Tools.DataProtection.UnitTests --filter "VaultListCommandTests"

# Run via the repo's test script
./eng/scripts/Test-Code.ps1 -Paths DataProtection
```

### Test Patterns

Each command test class validates:

1. **Success case** - Service returns data, response has `200 OK` status, results deserialize correctly
2. **Empty results** (list commands) - Service returns empty collection, response still succeeds
3. **Exception handling** - Service throws, response returns `500 InternalServerError` with troubleshooting link
4. **Missing parameters** - Each required parameter is omitted one at a time, response returns `400 BadRequest`

## Live Testing with MCP Inspector

The MCP Inspector provides an interactive UI for testing tools against live Azure resources.

### Step 1: Build the Server

```bash
# Build just the DataProtection project
dotnet build tools/Azure.Mcp.Tools.DataProtection/src

# Build the full Azure MCP Server (includes DataProtection)
dotnet build servers/Azure.Mcp.Server/src
```

### Step 2: Launch the MCP Inspector

```bash
# Option A: Namespace mode (default) - tools grouped by service
npx @modelcontextprotocol/inspector dotnet run --project servers/Azure.Mcp.Server/src -- service start --transport stdio

# Option B: All mode with dataprotection namespace - shows individual tools
npx @modelcontextprotocol/inspector dotnet run --project servers/Azure.Mcp.Server/src -- service start --transport stdio --mode all --namespace dataprotection
```

> **Tip:** In namespace mode, you'll see a single `dataprotection` namespace entry. Use `--mode all --namespace dataprotection` to see all 10 individual tools directly.

### Step 3: Verify Tool Registration

In the MCP Inspector, navigate to the **Tools** tab. You should see these 10 tools:

| Tool Name | Required Parameters |
|---|---|
| `dataprotection_vault_list` | `subscription` |
| `dataprotection_vault_get` | `subscription`, `resource-group`, `vault` |
| `dataprotection_backupinstance_list` | `subscription`, `resource-group`, `vault` |
| `dataprotection_backupinstance_get` | `subscription`, `resource-group`, `vault`, `backup-instance` |
| `dataprotection_policy_list` | `subscription`, `resource-group`, `vault` |
| `dataprotection_policy_get` | `subscription`, `resource-group`, `vault`, `policy` |
| `dataprotection_job_list` | `subscription`, `resource-group`, `vault` |
| `dataprotection_job_get` | `subscription`, `resource-group`, `vault`, `job` |
| `dataprotection_recoverypoint_list` | `subscription`, `resource-group`, `vault`, `backup-instance` |
| `dataprotection_recoverypoint_get` | `subscription`, `resource-group`, `vault`, `backup-instance`, `recovery-point` |

### Step 4: Test Each Command

Follow this sequence to validate end-to-end:

#### 4.1 List Vaults

```
Tool: dataprotection_vault_list
Parameters:
  subscription: <your-subscription-id>
```

Expected: JSON array of backup vaults with `name`, `location`, `provisioningState`, `storageType`.

#### 4.2 Get Vault

```
Tool: dataprotection_vault_get
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name-from-4.1>
```

Expected: Single vault object with full details.

#### 4.3 List Backup Instances

```
Tool: dataprotection_backupinstance_list
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name>
```

Expected: JSON array of backup instances with `name`, `dataSourceType`, `protectionStatus`, `currentProtectionState`.

#### 4.4 Get Backup Instance

```
Tool: dataprotection_backupinstance_get
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name>
  backup-instance: <instance-name-from-4.3>
```

Expected: Single backup instance object with full details.

#### 4.5 List Policies

```
Tool: dataprotection_policy_list
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name>
```

Expected: JSON array of backup policies with `name`, `dataSourceTypes`, `dataStoreDetails`.

#### 4.6 Get Policy

```
Tool: dataprotection_policy_get
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name>
  policy: <policy-name-from-4.5>
```

Expected: Single policy object with full details.

#### 4.7 List Jobs

```
Tool: dataprotection_job_list
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name>
```

Expected: JSON array of backup jobs with `name`, `operationType`, `status`, `startTime`, `endTime`. May return empty array if no recent jobs.

#### 4.8 Get Job

```
Tool: dataprotection_job_get
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name>
  job: <job-id-from-4.7>
```

Expected: Single job object with full details.

#### 4.9 List Recovery Points

```
Tool: dataprotection_recoverypoint_list
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name>
  backup-instance: <instance-name>
```

Expected: JSON array of recovery points with `name`, `type`, `recoveryPointTime`.

#### 4.10 Get Recovery Point

```
Tool: dataprotection_recoverypoint_get
Parameters:
  subscription: <your-subscription-id>
  resource-group: <resource-group-name>
  vault: <vault-name>
  backup-instance: <instance-name>
  recovery-point: <recovery-point-id-from-4.9>
```

Expected: Single recovery point object with full details.

## Setting Up Azure Test Resources

### Option A: Use Existing Resources

If you already have an Azure Backup vault with backup instances configured, use those resources directly. Gather the following values:

- **Subscription ID**: From `az account show --query id -o tsv`
- **Resource Group**: The resource group containing your vault
- **Vault Name**: Your backup vault name
- **Backup Instance Name**: A configured backup instance (from portal or `az dataprotection backup-instance list`)

### Option B: Deploy Test Resources with Bicep

The project includes a Bicep template for deploying test infrastructure:

```bash
# Login and set subscription
az login
az account set --subscription <your-subscription-id>

# Create a resource group
az group create --name rg-dataprotection-test --location eastus

# Deploy the test Backup Vault
az deployment group create \
  --resource-group rg-dataprotection-test \
  --template-file tools/Azure.Mcp.Tools.DataProtection/tests/test-resources.bicep \
  --parameters testApplicationOid=$(az ad signed-in-user show --query id -o tsv)
```

This creates a Backup Vault with locally redundant storage. You will need to manually configure backup instances and policies through the Azure Portal or CLI after deployment.

### Option C: Use the Repo's Test Resource Script

```powershell
# From repo root
Connect-AzAccount
./eng/scripts/Deploy-TestResources.ps1 -Paths DataProtection
```

This uses the standard repo infrastructure for deploying and configuring test resources.

## Environment Variables

The following environment variable can be set to avoid specifying `--subscription` on every command:

```bash
export AZURE_SUBSCRIPTION_ID=<your-subscription-id>
```

When set, the `--subscription` parameter becomes optional for all commands. The server uses this as a fallback when the parameter is not explicitly provided.

## Recorded (Playback) Tests

Recorded tests allow running live test scenarios in CI without needing Azure credentials. See [docs/recorded-tests.md](../../../docs/recorded-tests.md) for the full recorded test framework guide.

### Setting Up Recorded Tests for DataProtection

1. **Create test class** inheriting from `RecordedCommandTestsBase`
2. **Add `assets.json`** in the LiveTests project:
   ```json
   {
     "AssetsRepo": "Azure/azure-sdk-assets",
     "AssetsRepoPrefixPath": "",
     "TagPrefix": "Azure.Mcp.Tools.DataProtection",
     "Tag": ""
   }
   ```
3. **Record tests** with `TestMode: Record` in `.testsettings.json`
4. **Push recordings** via the test proxy CLI
5. **Switch to playback** and verify tests pass

### Recording Workflow

```powershell
# 1. Deploy test resources
./eng/scripts/Deploy-TestResources.ps1 -Paths DataProtection

# 2. Set TestMode to "Record" in .testsettings.json

# 3. Run live tests (records HTTP interactions)
dotnet test tools/Azure.Mcp.Tools.DataProtection/tests/Azure.Mcp.Tools.DataProtection.LiveTests

# 4. Verify recordings (locate the recording files)
./.proxy/Azure.Sdk.Tools.TestProxy.exe config locate -a tools/Azure.Mcp.Tools.DataProtection/tests/Azure.Mcp.Tools.DataProtection.LiveTests/assets.json

# 5. Switch TestMode to "Playback" and re-run tests

# 6. Push recordings to asset repo
./.proxy/Azure.Sdk.Tools.TestProxy.exe push -a tools/Azure.Mcp.Tools.DataProtection/tests/Azure.Mcp.Tools.DataProtection.LiveTests/assets.json
```

## Sharing with Peers

### Pull Request Workflow

1. **Push your branch** to the remote repository
2. **Create a PR** targeting `main`:
   ```bash
   gh pr create --title "Add Azure Data Protection MCP tools" --body "$(cat <<'EOF'
   ## Summary
   - Adds 10 read-only MCP commands for Azure Data Protection (Backup)
   - Covers Vault, Backup Instance, Policy, Job, and Recovery Point resources
   - Includes 59 unit tests with full coverage of success, error, and validation paths
   - Uses Azure.ResourceManager.DataProtectionBackup SDK v1.7.0

   ## Test plan
   - [ ] Unit tests pass (`dotnet test tools/Azure.Mcp.Tools.DataProtection/tests/Azure.Mcp.Tools.DataProtection.UnitTests`)
   - [ ] Full server builds (`dotnet build servers/Azure.Mcp.Server/src`)
   - [ ] Manual validation via MCP Inspector against live Azure subscription
   - [ ] Verify all 10 tools register correctly in MCP Inspector
   - [ ] Test each command with valid parameters against live resources

   ## Commands Added
   | Command | Description |
   |---|---|
   | `dataprotection_vault_list` | List all backup vaults in a subscription |
   | `dataprotection_vault_get` | Get details of a specific backup vault |
   | `dataprotection_backupinstance_list` | List backup instances in a vault |
   | `dataprotection_backupinstance_get` | Get details of a specific backup instance |
   | `dataprotection_policy_list` | List backup policies in a vault |
   | `dataprotection_policy_get` | Get details of a specific backup policy |
   | `dataprotection_job_list` | List backup jobs in a vault |
   | `dataprotection_job_get` | Get details of a specific backup job |
   | `dataprotection_recoverypoint_list` | List recovery points for a backup instance |
   | `dataprotection_recoverypoint_get` | Get details of a specific recovery point |
   EOF
   )"
   ```
3. **Share the PR link** with reviewers

### Peer Testing Steps

When a peer receives the PR or branch, they should follow this checklist:

1. **Checkout the branch**:
   ```bash
   git fetch origin
   git checkout users/shrja/CreateMCP
   ```

2. **Build the server**:
   ```bash
   dotnet build servers/Azure.Mcp.Server/src
   ```

3. **Run unit tests**:
   ```bash
   dotnet test tools/Azure.Mcp.Tools.DataProtection/tests/Azure.Mcp.Tools.DataProtection.UnitTests
   ```
   Expected: 59 tests passed.

4. **Authenticate to Azure**:
   ```bash
   az login
   az account set --subscription <subscription-with-backup-vaults>
   ```

5. **Launch MCP Inspector** and test commands:
   ```bash
   npx @modelcontextprotocol/inspector dotnet run --project servers/Azure.Mcp.Server/src -- service start --transport stdio --mode all --namespace dataprotection
   ```

6. **Walk through the test sequence** in [Step 4: Test Each Command](#step-4-test-each-command) above.

## Validation Checklist

Use this checklist to confirm the implementation is complete and correct:

- [ ] **Build**: `dotnet build servers/Azure.Mcp.Server/src` completes with 0 errors
- [ ] **Unit Tests**: All 59 tests pass
- [ ] **Tool Registration**: All 10 tools appear in MCP Inspector
- [ ] **Parameter Validation**: Commands reject requests when required parameters are missing
- [ ] **Vault List**: Returns vaults for a valid subscription
- [ ] **Vault Get**: Returns vault details with name, location, provisioning state
- [ ] **Backup Instance List**: Returns instances for a vault with backup configured
- [ ] **Backup Instance Get**: Returns instance with data source type, protection status
- [ ] **Policy List**: Returns policies for a vault with policies configured
- [ ] **Policy Get**: Returns policy with data source types, data store details
- [ ] **Job List**: Returns jobs (or empty list if none) for a vault
- [ ] **Job Get**: Returns job details with operation type, status, timestamps
- [ ] **Recovery Point List**: Returns recovery points for a backup instance
- [ ] **Recovery Point Get**: Returns recovery point with timestamp and type
- [ ] **Error Handling**: Invalid parameters return descriptive error messages with troubleshooting link
- [ ] **Authentication Failure**: Unauthenticated requests return clear authentication errors

## Troubleshooting

### Build Errors

**`NU1101: Unable to find package`**: Ensure `Azure.ResourceManager.DataProtectionBackup` (v1.7.0) is listed in `Directory.Packages.props`. The package name is `DataProtectionBackup`, not `DataProtection`.

**`CS1061: AsRequired not found`**: Ensure command files include `using Microsoft.Mcp.Core.Models.Option;` (not `Azure.Mcp.Core.Models.Option`).

### MCP Inspector Issues

**No tools visible**: Use `--mode all --namespace dataprotection` to bypass namespace proxy mode. In default namespace mode, you'll see only the `dataprotection` namespace entry, not individual tools.

**Server fails to start**: Verify .NET 10 SDK is installed (`dotnet --version`). The server requires .NET 10.

### Authentication Issues

**`AuthenticationFailedException`**: Run `az login` and ensure you have access to the target subscription. Verify `az account show` returns the correct subscription.

**`AuthorizationFailed`**: Ensure your account has at least Reader access to the Backup vault resources. The commands only perform read operations.

### No Results Returned

**Empty vault list**: Verify the subscription contains Azure Backup vaults. Use `az dataprotection backup-vault list --subscription <id>` to confirm.

**Empty backup instance list**: The vault must have at least one configured backup instance. Configure one through the Azure Portal under Backup center.

**Empty job list**: Jobs are created when backup/restore operations run. A newly created vault may not have any jobs yet.

## Project Structure

```
tools/Azure.Mcp.Tools.DataProtection/
├── src/
│   ├── Commands/
│   │   ├── BaseVaultCommand.cs              # Base for commands needing vault + resource group
│   │   ├── BaseBackupInstanceCommand.cs     # Base for commands needing backup instance
│   │   ├── DataProtectionJsonContext.cs     # JSON serialization context
│   │   ├── BackupInstance/
│   │   │   ├── BackupInstanceGetCommand.cs
│   │   │   └── BackupInstanceListCommand.cs
│   │   ├── Job/
│   │   │   ├── JobGetCommand.cs
│   │   │   └── JobListCommand.cs
│   │   ├── Policy/
│   │   │   ├── PolicyGetCommand.cs
│   │   │   └── PolicyListCommand.cs
│   │   ├── RecoveryPoint/
│   │   │   ├── RecoveryPointGetCommand.cs
│   │   │   └── RecoveryPointListCommand.cs
│   │   └── Vault/
│   │       ├── VaultGetCommand.cs
│   │       └── VaultListCommand.cs
│   ├── Models/
│   │   └── DataProtectionModels.cs          # BackupVaultModel, BackupInstanceModel, etc.
│   ├── Options/
│   │   ├── BaseDataProtectionOptions.cs
│   │   ├── DataProtectionOptionDefinitions.cs
│   │   ├── BackupInstance/
│   │   ├── Job/
│   │   ├── Policy/
│   │   ├── RecoveryPoint/
│   │   └── Vault/
│   ├── Services/
│   │   ├── DataProtectionService.cs         # SDK integration layer
│   │   └── IDataProtectionService.cs        # Service interface
│   ├── DataProtectionSetup.cs               # DI and command registration
│   └── Azure.Mcp.Tools.DataProtection.csproj
└── tests/
    ├── Azure.Mcp.Tools.DataProtection.UnitTests/
    │   ├── BackupInstance/
    │   ├── Job/
    │   ├── Policy/
    │   ├── RecoveryPoint/
    │   └── Vault/
    ├── Azure.Mcp.Tools.DataProtection.LiveTests/
    ├── test-resources.bicep                 # Bicep template for test vault
    ├── test-resources-post.ps1              # Post-deployment configuration
    └── README.md                            # This file
```
