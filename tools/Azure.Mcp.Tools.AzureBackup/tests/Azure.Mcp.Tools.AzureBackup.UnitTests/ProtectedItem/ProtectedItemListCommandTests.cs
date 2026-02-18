// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Commands;
using Azure.Mcp.Tools.AzureBackup.Commands.ProtectedItem;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.AzureBackup.UnitTests.ProtectedItem;

public class ProtectedItemListCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAzureBackupService _backupService;
    private readonly ILogger<ProtectedItemListCommand> _logger;
    private readonly ProtectedItemListCommand _command;
    private readonly CommandContext _context;
    private readonly Command _commandDefinition;

    public ProtectedItemListCommandTests()
    {
        _backupService = Substitute.For<IAzureBackupService>();
        _logger = Substitute.For<ILogger<ProtectedItemListCommand>>();

        var collection = new ServiceCollection().AddSingleton(_backupService);
        _serviceProvider = collection.BuildServiceProvider();
        _command = new(_logger);
        _context = new(_serviceProvider);
        _commandDefinition = _command.GetCommand();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsProtectedItems()
    {
        var expectedItems = new List<ProtectedItemInfo>
        {
            new("/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.RecoveryServices/vaults/vault1/backupFabrics/Azure/protectionContainers/container1/protectedItems/item1",
                "item1", "rsv", "Healthy", "AzureVM",
                "/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.Compute/virtualMachines/vm1",
                "DefaultPolicy", DateTimeOffset.Parse("2025-01-01T00:00:00Z"), "container1"),
            new("/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.DataProtection/backupVaults/vault1/backupInstances/item2",
                "item2", "dpp", "Healthy", "AzureBlob",
                "/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.Storage/storageAccounts/sa1",
                "BlobPolicy", DateTimeOffset.Parse("2025-01-02T00:00:00Z"), null)
        };

        _backupService.ListProtectedItemsAsync(
            Arg.Is("vault1"), Arg.Is("rg1"), Arg.Is("sub123"),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedItems));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);

        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.Status);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.ProtectedItemListCommandResult);

        Assert.NotNull(result);
        Assert.Equal(2, result.ProtectedItems.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_WhenNoProtectedItems()
    {
        _backupService.ListProtectedItemsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ProtectedItemInfo>()));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);

        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.Status);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.ProtectedItemListCommandResult);

        Assert.NotNull(result);
        Assert.Empty(result.ProtectedItems);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        _backupService.ListProtectedItemsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);

        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.Contains("Test error", response.Message);
    }

    [Fact]
    public void Constructor_InitializesCommandCorrectly()
    {
        var command = _command.GetCommand();
        Assert.Equal("list", command.Name);
        Assert.NotNull(command.Description);
    }
}
