// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Azure.Mcp.Tests;
using Azure.Mcp.Tests.Client;
using Azure.Mcp.Tests.Client.Helpers;
using Azure.Mcp.Tests.Generated.Models;
using Xunit;

namespace Azure.Mcp.Tools.AzureBackup.LiveTests;

public class AzureBackupCommandTests(ITestOutputHelper output, TestProxyFixture fixture, LiveServerFixture liveServerFixture) : RecordedCommandTestsBase(output, fixture, liveServerFixture)
{
    private string RsvName => $"{Settings.ResourceBaseName}-rsv";
    private string DppVaultName => $"{Settings.ResourceBaseName}-dpp";

    public override bool EnableDefaultSanitizerAdditions => false;

    public override List<UriRegexSanitizer> UriRegexSanitizers =>
    [
        new UriRegexSanitizer(new UriRegexSanitizerBody
        {
            Regex = "resource[gG]roups\\/([^?\\/]+)",
            Value = "sanitized",
            GroupForReplace = "1"
        })
    ];

    public override List<GeneralRegexSanitizer> GeneralRegexSanitizers =>
    [
        new GeneralRegexSanitizer(new GeneralRegexSanitizerBody()
        {
            Regex = Settings.ResourceGroupName,
            Value = "sanitized",
        }),
        new GeneralRegexSanitizer(new GeneralRegexSanitizerBody()
        {
            Regex = Settings.ResourceBaseName,
            Value = "sanitized",
        }),
        new GeneralRegexSanitizer(new GeneralRegexSanitizerBody()
        {
            Regex = Settings.SubscriptionId,
            Value = "00000000-0000-0000-0000-000000000000",
        }),
        new GeneralRegexSanitizer(new GeneralRegexSanitizerBody()
        {
            Regex = "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
            Value = "00000000-0000-0000-0000-000000000000",
        })
    ];

    // ── Vault List ──────────────────────────────────────────────

    [Fact]
    public async Task Should_list_vaults_in_subscription()
    {
        var result = await CallToolAsync(
            "azurebackup_vault_list",
            new()
            {
                { "subscription", Settings.SubscriptionId }
            });

        var vaults = result.AssertProperty("vaults");
        Assert.Equal(JsonValueKind.Array, vaults.ValueKind);
        Assert.NotEmpty(vaults.EnumerateArray());
    }

    [Fact]
    public async Task Should_list_rsv_vaults_with_type_filter()
    {
        var result = await CallToolAsync(
            "azurebackup_vault_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "vault-type", "rsv" }
            });

        var vaults = result.AssertProperty("vaults");
        Assert.Equal(JsonValueKind.Array, vaults.ValueKind);

        var vaultList = vaults.EnumerateArray().ToList();
        Assert.NotEmpty(vaultList);
        Assert.All(vaultList, v =>
        {
            var vaultType = v.GetProperty("vaultType").GetString();
            Assert.Equal("rsv", vaultType);
        });
    }

    [Fact]
    public async Task Should_list_dpp_vaults_with_type_filter()
    {
        var result = await CallToolAsync(
            "azurebackup_vault_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "vault-type", "dpp" }
            });

        var vaults = result.AssertProperty("vaults");
        Assert.Equal(JsonValueKind.Array, vaults.ValueKind);

        var vaultList = vaults.EnumerateArray().ToList();
        Assert.NotEmpty(vaultList);
        Assert.All(vaultList, v =>
        {
            var vaultType = v.GetProperty("vaultType").GetString();
            Assert.Equal("dpp", vaultType);
        });
    }

    // ── Vault Get ───────────────────────────────────────────────

    [Fact]
    public async Task Should_get_rsv_vault()
    {
        var result = await CallToolAsync(
            "azurebackup_vault_get",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", RsvName }
            });

        var vault = result.AssertProperty("vault");
        Assert.Equal(JsonValueKind.Object, vault.ValueKind);

        var vaultType = vault.GetProperty("vaultType").GetString();
        Assert.Equal("rsv", vaultType);

        var provisioningState = vault.GetProperty("provisioningState").GetString();
        Assert.Equal("Succeeded", provisioningState);
    }

    [Fact]
    public async Task Should_get_dpp_vault()
    {
        var result = await CallToolAsync(
            "azurebackup_vault_get",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", DppVaultName }
            });

        var vault = result.AssertProperty("vault");
        Assert.Equal(JsonValueKind.Object, vault.ValueKind);

        var vaultType = vault.GetProperty("vaultType").GetString();
        Assert.Equal("dpp", vaultType);

        var provisioningState = vault.GetProperty("provisioningState").GetString();
        Assert.Equal("Succeeded", provisioningState);
    }

    [Fact]
    public async Task Should_get_vault_with_explicit_type()
    {
        var result = await CallToolAsync(
            "azurebackup_vault_get",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", RsvName },
                { "vault-type", "rsv" }
            });

        var vault = result.AssertProperty("vault");
        Assert.Equal(JsonValueKind.Object, vault.ValueKind);
        Assert.Equal("rsv", vault.GetProperty("vaultType").GetString());
    }

    // ── Policy List ─────────────────────────────────────────────

    [Fact]
    public async Task Should_list_rsv_policies()
    {
        var result = await CallToolAsync(
            "azurebackup_policy_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", RsvName },
                { "vault-type", "rsv" }
            });

        var policies = result.AssertProperty("policies");
        Assert.Equal(JsonValueKind.Array, policies.ValueKind);
        // RSV vaults come with default policies
        Assert.NotEmpty(policies.EnumerateArray());
    }

    [Fact]
    public async Task Should_list_dpp_policies()
    {
        var result = await CallToolAsync(
            "azurebackup_policy_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", DppVaultName },
                { "vault-type", "dpp" }
            });

        var policies = result.AssertProperty("policies");
        Assert.Equal(JsonValueKind.Array, policies.ValueKind);
    }

    // ── Job List ────────────────────────────────────────────────

    [Fact]
    public async Task Should_list_rsv_jobs()
    {
        var result = await CallToolAsync(
            "azurebackup_job_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", RsvName },
                { "vault-type", "rsv" }
            });

        var jobs = result.AssertProperty("jobs");
        Assert.Equal(JsonValueKind.Array, jobs.ValueKind);
        // Jobs list may be empty for a fresh vault
    }

    [Fact]
    public async Task Should_list_dpp_jobs()
    {
        var result = await CallToolAsync(
            "azurebackup_job_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", DppVaultName },
                { "vault-type", "dpp" }
            });

        var jobs = result.AssertProperty("jobs");
        Assert.Equal(JsonValueKind.Array, jobs.ValueKind);
    }

    // ── Protected Item List ─────────────────────────────────────

    [Fact]
    public async Task Should_list_rsv_protected_items()
    {
        var result = await CallToolAsync(
            "azurebackup_protecteditem_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", RsvName },
                { "vault-type", "rsv" }
            });

        var items = result.AssertProperty("protectedItems");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        // May be empty for a fresh vault
    }

    [Fact]
    public async Task Should_list_dpp_protected_items()
    {
        var result = await CallToolAsync(
            "azurebackup_protecteditem_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", DppVaultName },
                { "vault-type", "dpp" }
            });

        var items = result.AssertProperty("protectedItems");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    // ── Recovery Point List ─────────────────────────────────────

    [Fact]
    public async Task Should_return_error_for_nonexistent_vault()
    {
        var result = await CallToolAsync(
            "azurebackup_vault_get",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "vault", "nonexistent-vault-name" }
            });

        Assert.NotNull(result);
        Assert.True(result.Value.TryGetProperty("message", out _));
    }
}
