param(
    [string] $TenantId,
    [string] $TestApplicationId,
    [string] $ResourceGroupName,
    [string] $BaseName,
    [hashtable] $DeploymentOutputs,
    [hashtable] $AdditionalParameters
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot/../../../eng/common/scripts/common.ps1"
. "$PSScriptRoot/../../../eng/scripts/helpers/TestResourcesHelpers.ps1"

$testSettings = New-TestSettings @PSBoundParameters -OutputPath $PSScriptRoot

Write-Host "Azure Backup test resources deployed successfully" -ForegroundColor Green
Write-Host "  RSV Name: $($DeploymentOutputs['RSVNAME'].Value)" -ForegroundColor Cyan
Write-Host "  DPP Vault Name: $($DeploymentOutputs['DPPVAULTNAME'].Value)" -ForegroundColor Cyan
Write-Host "  Resource Group: $($DeploymentOutputs['RESOURCEGROUPNAME'].Value)" -ForegroundColor Cyan
Write-Host "  Location: $($DeploymentOutputs['LOCATION'].Value)" -ForegroundColor Cyan

Write-Host ""
Write-Host "Test settings written to: $PSScriptRoot/.testsettings.json" -ForegroundColor Green
Write-Host ""
Write-Host "To run live tests:" -ForegroundColor Yellow
Write-Host "  ./eng/scripts/Test-Code.ps1 -TestType Live -Paths AzureBackup" -ForegroundColor Cyan
