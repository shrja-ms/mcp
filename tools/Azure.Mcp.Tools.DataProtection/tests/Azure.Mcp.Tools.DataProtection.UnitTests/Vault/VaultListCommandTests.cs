// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Azure.Mcp.Core.Options;
using Azure.Mcp.Tools.DataProtection.Commands;
using Azure.Mcp.Tools.DataProtection.Commands.Vault;
using Azure.Mcp.Tools.DataProtection.Models;
using Azure.Mcp.Tools.DataProtection.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Models.Command;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.DataProtection.UnitTests.Vault;

public class VaultListCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataProtectionService _service;
    private readonly ILogger<VaultListCommand> _logger;

    public VaultListCommandTests()
    {
        _service = Substitute.For<IDataProtectionService>();
        _logger = Substitute.For<ILogger<VaultListCommand>>();

        var collection = new ServiceCollection();
        collection.AddSingleton(_service);
        _serviceProvider = collection.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsVaults_WhenVaultsExist()
    {
        var expectedVaults = new BackupVaultModel[] { new() { Name = "vault1" }, new() { Name = "vault2" } };
        _service.ListVaultsAsync("sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expectedVaults);

        var command = new VaultListCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123"]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.NotNull(response.Results);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, DataProtectionJsonContext.Default.VaultListCommandResult);

        Assert.NotNull(result);
        Assert.Collection(result.Vaults,
            item => Assert.Equal("vault1", item.Name),
            item => Assert.Equal("vault2", item.Name));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_WhenNoVaults()
    {
        _service.ListVaultsAsync("sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BackupVaultModel>());

        var command = new VaultListCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123"]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.NotNull(response.Results);

        var json = JsonSerializer.Serialize(response.Results);
        var result = JsonSerializer.Deserialize(json, DataProtectionJsonContext.Default.VaultListCommandResult);

        Assert.NotNull(result);
        Assert.Empty(result.Vaults);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesException()
    {
        var expectedError = "Test error. To mitigate this issue, please refer to the troubleshooting guidelines here at https://aka.ms/azmcp/troubleshooting.";
        _service.ListVaultsAsync("sub123", Arg.Any<string?>(), Arg.Any<RetryPolicyOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Test error"));

        var command = new VaultListCommand(_logger);
        var args = command.GetCommand().Parse(["--subscription", "sub123"]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.Equal(expectedError, response.Message);
    }

    [Theory]
    [InlineData("--subscription")]
    public async Task ExecuteAsync_ReturnsError_WhenParameterIsMissing(string missingParameter)
    {
        var command = new VaultListCommand(_logger);
        var argsList = new List<string>();
        if (missingParameter != "--subscription")
        {
            argsList.Add("--subscription");
            argsList.Add("sub123");
        }

        var args = command.GetCommand().Parse([.. argsList]);
        var context = new CommandContext(_serviceProvider);
        var response = await command.ExecuteAsync(context, args, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal($"Missing Required options: {missingParameter}", response.Message);
    }
}
