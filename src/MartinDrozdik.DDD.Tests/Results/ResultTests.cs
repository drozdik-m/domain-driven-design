using MartinDrozdik.DDD.Results;
using MartinDrozdik.DDD.Results.Exceptions;

namespace MartinDrozdik.DDD.Tests.Results;

public class ResultTests
{
    private const string Error = "boom";

    [Fact]
    public void Success_exposes_its_value()
    {
        // Arrange
        var result = Result.Success<int, string>(42);

        // Act
        var value = result.Value;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, value);
    }

    [Fact]
    public void Success_throws_when_the_error_is_accessed()
    {
        // Arrange
        var result = Result.Success<int, string>(42);

        // Act
        var act = () => { _ = result.Error; };

        // Assert
        Assert.Throws<ResultSuccessException>(act);
    }

    [Fact]
    public void Failure_exposes_its_error()
    {
        // Arrange
        var result = Result.Failure<int, string>(Error);

        // Act
        var error = result.Error;

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(Error, error);
    }

    [Fact]
    public void Failure_throws_when_the_value_is_accessed()
    {
        // Arrange
        var result = Result.Failure<int, string>(Error);

        // Act
        var act = () => { _ = result.Value; };

        // Assert
        var exception = Assert.Throws<ResultFailureException<string>>(act);
        Assert.Equal(Error, exception.Error);
        Assert.Contains(Error, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Failure_throws_when_no_error_is_provided()
    {
        // Arrange
        string? missingError = null;

        // Act
        var act = () => { _ = Result.Failure<int, string>(missingError!); };

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Implicit_conversion_from_a_value_creates_a_success()
    {
        // Arrange & Act
        Result<int, string> result = 42;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Implicit_conversion_from_an_error_creates_a_failure()
    {
        // Arrange & Act
        Result<int, string> result = Error;

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(Error, result.Error);
    }

    [Fact]
    public void Implicit_conversion_to_a_unit_result_keeps_the_state()
    {
        // Arrange
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>(Error);

        // Act
        UnitResult<string> convertedSuccess = success;
        UnitResult<string> convertedFailure = failure;

        // Assert
        Assert.True(convertedSuccess.IsSuccess);
        Assert.True(convertedFailure.IsFailure);
        Assert.Equal(Error, convertedFailure.Error);
    }

    [Fact]
    public void Default_instance_is_a_success_carrying_the_default_value()
    {
        // Arrange & Act
        var result = default(Result<int, string>);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void GetValueOrDefault_returns_the_fallback_for_a_failure()
    {
        // Arrange
        var result = Result.Failure<int, string>(Error);

        // Act
        var fallback = result.GetValueOrDefault(7);
        var defaultValue = result.GetValueOrDefault();

        // Assert
        Assert.Equal(7, fallback);
        Assert.Equal(0, defaultValue);
    }

    [Fact]
    public void TryGetValue_returns_true_and_the_value_for_a_success()
    {
        // Arrange
        var result = Result.Success<int, string>(42);

        // Act
        var succeeded = result.TryGetValue(out var value);
        var failed = result.TryGetError(out _);

        // Assert
        Assert.True(succeeded);
        Assert.Equal(42, value);
        Assert.False(failed);
    }

    [Fact]
    public void TryGetError_returns_true_and_the_error_for_a_failure()
    {
        // Arrange
        var result = Result.Failure<int, string>(Error);

        // Act
        var failed = result.TryGetError(out var error);
        var succeeded = result.TryGetValue(out _);

        // Assert
        Assert.True(failed);
        Assert.Equal(Error, error);
        Assert.False(succeeded);
    }

    [Fact]
    public void Deconstruct_splits_the_result_into_a_value_and_an_error()
    {
        // Arrange
        var result = Result.Failure<int, string>(Error);

        // Act
        var (value, error) = result;

        // Assert
        Assert.Equal(0, value);
        Assert.Equal(Error, error);
    }

    [Fact]
    public void Equals_successfully_returns_true_for_equal_parameters()
    {
        // Arrange
        var firstSuccess = Result.Success<int, string>(42);
        var secondSuccess = Result.Success<int, string>(42);
        var firstFailure = Result.Failure<int, string>(Error);
        var secondFailure = Result.Failure<int, string>(Error);

        // Act
        var successesEqual = firstSuccess == secondSuccess;
        var failuresEqual = firstFailure == secondFailure;

        // Assert
        Assert.True(successesEqual);
        Assert.True(failuresEqual);
        Assert.Equal(firstSuccess.GetHashCode(), secondSuccess.GetHashCode());
        Assert.Equal(firstFailure.GetHashCode(), secondFailure.GetHashCode());
    }

    [Fact]
    public void Equals_returns_false_for_different_parameters()
    {
        // Arrange
        var success = Result.Success<int, string>(42);
        var otherSuccess = Result.Success<int, string>(7);
        var failure = Result.Failure<int, string>(Error);

        // Act
        var differentValues = success != otherSuccess;
        var differentStates = success != failure;

        // Assert
        Assert.True(differentValues);
        Assert.True(differentStates);
    }

    [Fact]
    public void ToString_describes_the_state_of_the_result()
    {
        // Arrange
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>(Error);

        // Act
        var successText = success.ToString();
        var failureText = failure.ToString();

        // Assert
        Assert.Equal("Success(42)", successText);
        Assert.Equal($"Failure({Error})", failureText);
    }
}
