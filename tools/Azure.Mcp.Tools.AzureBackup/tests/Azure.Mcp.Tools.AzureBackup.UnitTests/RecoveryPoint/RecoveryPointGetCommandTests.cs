// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Commands;
using Azure.Mcp.Tools.AzureBackup.Commands.RecoveryPoint;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.AzureBackup.UnitTests.RecoveryPoint;

public class RecoveryPointGetCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAzureBackupService _backupService;
    private readonly ILogger<RecoveryPointGetCommand> _logger;
    private readonly RecoveryPointGetCommand _command;
    private readonly CommandContext _context;
    private readonly Command _commandDefinition;

    public RecoveryPointGetCommandTests()
    {
        _backupService = Substitute.For<IAzureBackupService>();
        _logger = Substitute.For<ILogger<RecoveryPointGetCommand>>();

        var collection = new ServiceCollection().AddSingleton(_backupService);
        _serviceProvider = collection.BuildServiceProvider();
        _command = new(_logger);
        _context = new(_serviceProvider);
        _commandDefinition = _command.GetCommand();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsRecoveryPointDetails_WhenRecoveryPointExists()
    {
        // Arrange
        var expectedRecoveryPoint = new RecoveryPointInfo(
            "/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.RecoveryServices/vaults/vault1/backupFabrics/Azure/protectionContainers/container1/protectedItems/item1/recoveryPoints/rp1",
            "rp1", "rsv", DateTimeOffset.Parse("2025-01-01T00:00:00Z"), "Full");

        _backupService.GetRecoveryPointAsync(
            Arg.Is("vault1"), Arg.Is("rg1"), Arg.Is("sub123"),
            Arg.Is("item1"), Arg.Is("rp1"),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedRecoveryPoint));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--protected-item", "item1", "--recovery-point", "rp1"]);

        // Act
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.NotNull(response.Results);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.RecoveryPointGetCommandResult);

        Assert.NotNull(result);
        Assert.NotNull(result.RecoveryPoint);
        Assert.Equal("rp1", result.RecoveryPoint.Name);
        Assert.Equal("rsv", result.RecoveryPoint.VaultType);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesNotFound()
    {
        _backupService.GetRecoveryPointAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Recovery point not found"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--protected-item", "item1", "--recovery-point", "rp1"]);

        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.Status);
        Assert.Contains("Recovery point not found", response.Message);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        _backupService.GetRecoveryPointAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--protected-item", "item1", "--recovery-point", "rp1"]);

        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.Contains("Test error", response.Message);
    }

    [Fact]
    public void Constructor_InitializesCommandCorrectly()
    {
        var command = _command.GetCommand();
        Assert.Equal("get", command.Name);
        Assert.NotNull(command.Description);
        Assert.NotEmpty(command.Description);
    }
}
