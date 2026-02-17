// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.DataProtection.Commands;
using Azure.Mcp.Tools.DataProtection.Commands.Policy;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.DataProtection.UnitTests.Policy;

public class PolicyGetCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataProtectionService _service;
    private readonly ILogger<PolicyGetCommand> _logger;

    public PolicyGetCommandTests()
    {
        _service = Substitute.For<IDataProtectionService>();
        _logger = Substitute.For<ILogger<PolicyGetCommand>>();
        var collection = new ServiceCollection();
        collection.AddSingleton(_service);
        _serviceProvider = collection.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsPolicy_WhenPolicyExists()
    {
        var expected = new BackupPolicyModel { Name = "policy1", DataSourceType = "AzureBlob" };
        _service.GetPolicyAsync("policy1", "vault1", "rg1", "sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var command = new PolicyGetCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--policy", "policy1"]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.NotNull(response.Results);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, DataProtectionJsonContext.Default.PolicyGetCommandResult);
        Assert.NotNull(result);
        Assert.Equal("policy1", result.Policy.Name);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        var expectedError = "Test error. To mitigate this issue, please refer to the troubleshooting guidelines here at https://aka.ms/azmcp/troubleshooting.";
        _service.GetPolicyAsync("policy1", "vault1", "rg1", "sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var command = new PolicyGetCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--policy", "policy1"]);
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
    [InlineData("--policy")]
    public async Task ExecuteAsync_ReturnsError_WhenParameterIsMissing(string missingParameter)
    {
        var command = new PolicyGetCommand(_logger);
        var argsList = new List<string>();
        if (missingParameter != "--subscription") { argsList.Add("--subscription"); argsList.Add("sub123"); }
        if (missingParameter != "--resource-group") { argsList.Add("--resource-group"); argsList.Add("rg1"); }
        if (missingParameter != "--vault") { argsList.Add("--vault"); argsList.Add("vault1"); }
        if (missingParameter != "--policy") { argsList.Add("--policy"); argsList.Add("policy1"); }

        var args = command.GetCommand().Parse([.. argsList]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
    }
}
