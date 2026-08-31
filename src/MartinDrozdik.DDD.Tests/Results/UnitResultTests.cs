using MartinDrozdik.DDD.Results;
using MartinDrozdik.DDD.Results.Exceptions;

namespace MartinDrozdik.DDD.Tests.Results;

public class UnitResultTests
{
    private const string Error = "boom";

    [Fact]
    public void Success_is_successful()
    {
        // Arrange & Act
        var result = UnitResult.Success<string>();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Default_instance_is_a_success()
    {
        // Arrange & Act
        var result = default(UnitResult<string>);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Failure_exposes_its_error()
    {
        // Arrange & Act
        var result = UnitResult.Failure(Error);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(Error, result.Error);
    }

    [Fact]
    public void Success_throws_when_the_error_is_accessed()
    {
        // Arrange
        var result = UnitResult.Success<string>();

        // Act
        var act = () => { _ = result.Error; };

        // Assert
        Assert.Throws<ResultSuccessException>(act);
    }

    [Fact]
    public void Failure_throws_when_no_error_is_provided()
    {
        // Arrange
        string? missingError = null;

        // Act
        var act = () => { _ = UnitResult.Failure(missingError!); };

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Implicit_conversion_from_an_error_creates_a_failure()
    {
        // Arrange & Act
        UnitResult<string> result = Error;

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(Error, result.Error);
    }

    [Fact]
    public void TryGetError_returns_true_and_the_error_for_a_failure()
    {
        // Arrange
        var failure = UnitResult.Failure(Error);
        var success = UnitResult.Success<string>();

        // Act
        var failed = failure.TryGetError(out var error);
        var succeeded = success.TryGetError(out _);

        // Assert
        Assert.True(failed);
        Assert.Equal(Error, error);
        Assert.False(succeeded);
    }

    [Fact]
    public void Deconstruct_splits_the_result_into_a_state_and_an_error()
    {
        // Arrange
        var result = UnitResult.Failure(Error);

        // Act
        var (isSuccess, error) = result;

        // Assert
        Assert.False(isSuccess);
        Assert.Equal(Error, error);
    }

    [Fact]
    public void Equals_successfully_returns_true_for_equal_parameters()
    {
        // Arrange
        var firstSuccess = UnitResult.Success<string>();
        var secondSuccess = UnitResult.Success<string>();
        var firstFailure = UnitResult.Failure(Error);
        var secondFailure = UnitResult.Failure(Error);

        // Act
        var successesEqual = firstSuccess == secondSuccess;
        var failuresEqual = firstFailure == secondFailure;

        // Assert
        Assert.True(successesEqual);
        Assert.True(failuresEqual);
        Assert.Equal(firstFailure.GetHashCode(), secondFailure.GetHashCode());
    }

    [Fact]
    public void Equals_returns_false_for_different_parameters()
    {
        // Arrange
        var success = UnitResult.Success<string>();
        var failure = UnitResult.Failure(Error);
        var otherFailure = UnitResult.Failure("other");

        // Act
        var differentStates = success != failure;
        var differentErrors = failure != otherFailure;

        // Assert
        Assert.True(differentStates);
        Assert.True(differentErrors);
    }

    [Fact]
    public void ToString_describes_the_state_of_the_result()
    {
        // Arrange
        var success = UnitResult.Success<string>();
        var failure = UnitResult.Failure(Error);

        // Act
        var successText = success.ToString();
        var failureText = failure.ToString();

        // Assert
        Assert.Equal("Success", successText);
        Assert.Equal($"Failure({Error})", failureText);
    }
}
