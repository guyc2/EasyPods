using EasyPods.Model.Common;
using Xunit;

namespace EasyPods.Test.ModelTests;

public class ResultTests
{
    [Fact]
    public void Success_ReturnsValue_AndIsSuccessTrue()
    {
        var result = Result<string>.Success("Connected");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("Connected", result.Value);
    }

    [Fact]
    public void Fail_ReturnsError_AndThrowsOnValueAccess()
    {
        var failure = new ConnectionFailedFailure("Timeout");
        var result = Result<string>.Fail(failure);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(failure, result.Error);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Match_ExecutesSuccessBranch_WhenSuccess()
    {
        var result = Result<int>.Success(42);

        var output = result.Match(
            onSuccess: val => $"Value: {val}",
            onFailure: err => $"Error: {err.Message}");

        Assert.Equal("Value: 42", output);
    }

    [Fact]
    public void Match_ExecutesFailureBranch_WhenFailure()
    {
        var result = Result<int>.Fail(new DeviceNotFoundFailure("Lost"));

        var output = result.Match(
            onSuccess: val => $"Value: {val}",
            onFailure: err => $"Error: {err.Message}");

        Assert.Equal("Error: Lost", output);
    }
}
