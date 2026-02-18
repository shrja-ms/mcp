// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.AzureBackup.Commands;
using Azure.Mcp.Tools.AzureBackup.Commands.Job;
using Azure.Mcp.Tools.AzureBackup.Models;
using Azure.Mcp.Tools.AzureBackup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.AzureBackup.UnitTests.Job;

public class JobListCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAzureBackupService _backupService;
    private readonly ILogger<JobListCommand> _logger;
    private readonly JobListCommand _command;
    private readonly CommandContext _context;
    private readonly Command _commandDefinition;

    public JobListCommandTests()
    {
        _backupService = Substitute.For<IAzureBackupService>();
        _logger = Substitute.For<ILogger<JobListCommand>>();

        var collection = new ServiceCollection().AddSingleton(_backupService);
        _serviceProvider = collection.BuildServiceProvider();
        _command = new(_logger);
        _context = new(_serviceProvider);
        _commandDefinition = _command.GetCommand();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsJobs()
    {
        var expectedJobs = new List<BackupJobInfo>
        {
            new("/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.RecoveryServices/vaults/vault1/backupJobs/job1",
                "job1", "rsv", "Backup", "Completed",
                DateTimeOffset.Parse("2025-01-01T00:00:00Z"), DateTimeOffset.Parse("2025-01-01T01:00:00Z"),
                "AzureVM", "myvm1"),
            new("/subscriptions/sub123/resourceGroups/rg1/providers/Microsoft.RecoveryServices/vaults/vault1/backupJobs/job2",
                "job2", "rsv", "Restore", "InProgress",
                DateTimeOffset.Parse("2025-01-02T00:00:00Z"), null,
                "AzureVM", "myvm2")
        };

        _backupService.ListJobsAsync(
            Arg.Is("vault1"), Arg.Is("rg1"), Arg.Is("sub123"),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedJobs));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);

        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.Status);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.JobListCommandResult);

        Assert.NotNull(result);
        Assert.Equal(2, result.Jobs.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_WhenNoJobs()
    {
        _backupService.ListJobsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BackupJobInfo>()));

        var args = _commandDefinition.Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);

        var response = await _command.ExecuteAsync(_context, args, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.Status);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, AzureBackupJsonContext.Default.JobListCommandResult);

        Assert.NotNull(result);
        Assert.Empty(result.Jobs);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        _backupService.ListJobsAsync(
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
