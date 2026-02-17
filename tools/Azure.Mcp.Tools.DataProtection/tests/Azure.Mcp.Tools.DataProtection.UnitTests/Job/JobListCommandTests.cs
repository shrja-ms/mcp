// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.DataProtection.Commands;
using Azure.Mcp.Tools.DataProtection.Commands.Job;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.DataProtection.UnitTests.Job;

public class JobListCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataProtectionService _service;
    private readonly ILogger<JobListCommand> _logger;

    public JobListCommandTests()
    {
        _service = Substitute.For<IDataProtectionService>();
        _logger = Substitute.For<ILogger<JobListCommand>>();
        var collection = new ServiceCollection();
        collection.AddSingleton(_service);
        _serviceProvider = collection.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsJobs_WhenJobsExist()
    {
        var expected = new BackupJobModel[] { new() { Name = "job1" }, new() { Name = "job2" } };
        _service.ListJobsAsync("vault1", "rg1", "sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var command = new JobListCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.NotNull(response.Results);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, DataProtectionJsonContext.Default.JobListCommandResult);
        Assert.NotNull(result);
        Assert.Equal(2, result.Jobs.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_WhenNoJobs()
    {
        _service.ListJobsAsync("vault1", "rg1", "sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BackupJobModel>());

        var command = new JobListCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.NotNull(response.Results);
        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, DataProtectionJsonContext.Default.JobListCommandResult);
        Assert.NotNull(result);
        Assert.Empty(result.Jobs);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        var expectedError = "Test error. To mitigate this issue, please refer to the troubleshooting guidelines here at https://aka.ms/azmcp/troubleshooting.";
        _service.ListJobsAsync("vault1", "rg1", "sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var command = new JobListCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1"]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.Equal(expectedError, response.Message);
    }

    [Theory]
    [InlineData("--subscription")]
    [InlineData("--resource-group")]
    [InlineData("--vault")]
    public async Task ExecuteAsync_ReturnsError_WhenParameterIsMissing(string missingParameter)
    {
        var command = new JobListCommand(_logger);
        var argsList = new List<string>();
        if (missingParameter != "--subscription") { argsList.Add("--subscription"); argsList.Add("sub123"); }
        if (missingParameter != "--resource-group") { argsList.Add("--resource-group"); argsList.Add("rg1"); }
        if (missingParameter != "--vault") { argsList.Add("--vault"); argsList.Add("vault1"); }

        var args = command.GetCommand().Parse([.. argsList]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
    }
}
