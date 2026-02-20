// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Commands;
using Azure.Mcp.Tools.AzureBackup.Commands.Backup;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.AzureBackup.UnitTests.Backup;

public class BackupStatusCommandTests
{
    private readonly IAzureBackupService _backupService;
    private readonly BackupStatusCommand _command;
    private readonly CommandContext _context;
    private readonly Command _commandDefinition;

    public BackupStatusCommandTests()
    {
        _backupService = Substitute.For<IAzureBackupService>();
        var logger = Substitute.For<ILogger<BackupStatusCommand>>();
        var collection = new ServiceCollection().AddSingleton(_backupService);
        _context = new(collection.BuildServiceProvider());
        _command = new(logger);
        _commandDefinition = _command.GetCommand();
    }

    [Fact]
    public async Task ExecuteAsync_GetsBackupStatus_Successfully()
    {
        var expectedResult = new BackupStatusResult("/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.Compute/virtualMachines/vm1", "Protected", "vault1", "DefaultPolicy", DateTimeOffset.UtcNow, "Completed", "Healthy");
        _backupService.GetBackupStatusAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--datasource-id", "/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.Compute/virtualMachines/vm1", "--location", "eastus"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.BackupStatusCommandResult);
        Assert.NotNull(result);
        Assert.Equal("Protected", result.Status.ProtectionStatus);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesNotFound()
    {
        _backupService.GetBackupStatusAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Not found"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--datasource-id", "ds1", "--location", "eastus"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        _backupService.GetBackupStatusAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--datasource-id", "ds1", "--location", "eastus"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
    }
}
