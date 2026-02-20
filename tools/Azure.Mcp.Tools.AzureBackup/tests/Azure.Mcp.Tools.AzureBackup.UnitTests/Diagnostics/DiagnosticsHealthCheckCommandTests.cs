// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Commands;
using Azure.Mcp.Tools.AzureBackup.Commands.Diagnostics;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.AzureBackup.UnitTests.Diagnostics;

public class DiagnosticsHealthCheckCommandTests
{
    private readonly IAzureBackupService _backupService;
    private readonly DiagnosticsHealthCheckCommand _command;
    private readonly CommandContext _context;
    private readonly Command _commandDefinition;

    public DiagnosticsHealthCheckCommandTests()
    {
        _backupService = Substitute.For<IAzureBackupService>();
        var logger = Substitute.For<ILogger<DiagnosticsHealthCheckCommand>>();
        var collection = new ServiceCollection().AddSingleton(_backupService);
        _context = new(collection.BuildServiceProvider());
        _command = new(logger);
        _commandDefinition = _command.GetCommand();
    }

    [Fact]
    public async Task ExecuteAsync_RunsHealthCheck_Successfully()
    {
        var expectedResult = new HealthCheckResult(
            "vault1", "rsv", 10, 10, 0, 0,
            "Enabled", "Unlocked", "Microsoft-managed",
            new List<HealthCheckItemDetail>());
        _backupService.RunBackupHealthCheckAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.DiagnosticsHealthCheckCommandResult);
        Assert.NotNull(result);
        Assert.Equal(10, result.HealthCheck.TotalProtectedItems);
        Assert.Equal(0, result.HealthCheck.UnhealthyItems);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesNotFound()
    {
        _backupService.RunBackupHealthCheckAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Not found"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        _backupService.RunBackupHealthCheckAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
    }
}
