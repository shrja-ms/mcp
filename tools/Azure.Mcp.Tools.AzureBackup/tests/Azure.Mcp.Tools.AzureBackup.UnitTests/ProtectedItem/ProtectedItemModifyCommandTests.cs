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

public class ProtectedItemModifyCommandTests
{
    private readonly IAzureBackupService _backupService;
    private readonly ProtectedItemModifyCommand _command;
    private readonly CommandContext _context;
    private readonly Command _commandDefinition;

    public ProtectedItemModifyCommandTests()
    {
        _backupService = Substitute.For<IAzureBackupService>();
        var logger = Substitute.For<ILogger<ProtectedItemModifyCommand>>();
        var collection = new ServiceCollection().AddSingleton(_backupService);
        _context = new(collection.BuildServiceProvider());
        _command = new(logger);
        _commandDefinition = _command.GetCommand();
    }

    [Fact]
    public async Task ExecuteAsync_ModifiesProtection_Successfully()
    {
        var expectedResult = new OperationResult("Succeeded", null, "Protection modified");
        _backupService.ModifyProtectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--protected-item", "item1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.ProtectedItemModifyCommandResult);
        Assert.NotNull(result);
        Assert.Equal("Succeeded", result.Result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesNotFound()
    {
        _backupService.ModifyProtectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Not found"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--protected-item", "item1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        _backupService.ModifyProtectionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--protected-item", "item1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
    }
}
