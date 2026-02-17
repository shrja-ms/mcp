// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.DataProtection.Commands;
using Azure.Mcp.Tools.DataProtection.Commands.RecoveryPoint;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.DataProtection.UnitTests.RecoveryPoint;

public class RecoveryPointGetCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataProtectionService _service;
    private readonly ILogger<RecoveryPointGetCommand> _logger;

    public RecoveryPointGetCommandTests()
    {
        _service = Substitute.For<IDataProtectionService>();
        _logger = Substitute.For<ILogger<RecoveryPointGetCommand>>();
        var collection = new ServiceCollection();
        collection.AddSingleton(_service);
        _serviceProvider = collection.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsRecoveryPoint_WhenRecoveryPointExists()
    {
        var expected = new RecoveryPointModel { Name = "rp1", RecoveryPointType = "Full" };
        _service.GetRecoveryPointAsync("rp1", "instance1", "vault1", "rg1", "sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var command = new RecoveryPointGetCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--backup-instance", "instance1", "--recovery-point", "rp1"]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.NotNull(response.Results);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, DataProtectionJsonContext.Default.RecoveryPointGetCommandResult);
        Assert.NotNull(result);
        Assert.Equal("rp1", result.RecoveryPoint.Name);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        var expectedError = "Test error. To mitigate this issue, please refer to the troubleshooting guidelines here at https://aka.ms/azmcp/troubleshooting.";
        _service.GetRecoveryPointAsync("rp1", "instance1", "vault1", "rg1", "sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var command = new RecoveryPointGetCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123", "--resource-group", "rg1", "--vault", "vault1", "--backup-instance", "instance1", "--recovery-point", "rp1"]);
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
    [InlineData("--backup-instance")]
    [InlineData("--recovery-point")]
    public async Task ExecuteAsync_ReturnsError_WhenParameterIsMissing(string missingParameter)
    {
        var command = new RecoveryPointGetCommand(_logger);
        var argsList = new List<string>();
        if (missingParameter != "--subscription") { argsList.Add("--subscription"); argsList.Add("sub123"); }
        if (missingParameter != "--resource-group") { argsList.Add("--resource-group"); argsList.Add("rg1"); }
        if (missingParameter != "--vault") { argsList.Add("--vault"); argsList.Add("vault1"); }
        if (missingParameter != "--backup-instance") { argsList.Add("--backup-instance"); argsList.Add("instance1"); }
        if (missingParameter != "--recovery-point") { argsList.Add("--recovery-point"); argsList.Add("rp1"); }

        var args = command.GetCommand().Parse([.. argsList]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
    }
}
