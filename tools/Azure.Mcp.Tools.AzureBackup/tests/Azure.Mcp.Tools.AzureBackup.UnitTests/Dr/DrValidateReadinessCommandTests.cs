// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Commands;
using Azure.Mcp.Tools.AzureBackup.Commands.Dr;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.AzureBackup.UnitTests.Dr;

public class DrValidateReadinessCommandTests
{
    private readonly IAzureBackupService _backupService;
    private readonly DrValidateReadinessCommand _command;
    private readonly CommandContext _context;
    private readonly Command _commandDefinition;

    public DrValidateReadinessCommandTests()
    {
        _backupService = Substitute.For<IAzureBackupService>();
        var logger = Substitute.For<ILogger<DrValidateReadinessCommand>>();
        var collection = new ServiceCollection().AddSingleton(_backupService);
        _context = new(collection.BuildServiceProvider());
        _command = new(logger);
        _commandDefinition = _command.GetCommand();
    }

    [Fact]
    public async Task ExecuteAsync_ValidatesDrReadiness_Successfully()
    {
        var expectedResult = new DrValidationResult("vault1", true, "GeoRedundant", "eastus", "westus", 5, 5, "All items have secondary recovery points");
        _backupService.ValidateDrReadinessAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.DrValidateReadinessCommandResult);
        Assert.NotNull(result);
        Assert.True(result.Validation.CrrEnabled);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesNotFound()
    {
        _backupService.ValidateDrReadinessAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Not found"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.Status);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        _backupService.ValidateDrReadinessAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
    }
}
