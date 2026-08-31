using MartinDrozdik.DDD.Results;

namespace MartinDrozdik.DDD.Tests.Results;

public class ResultExtensionsTests
{
    private const string Error = "boom";

    [Fact]
    public void Map_projects_the_value_of_a_success()
    {
        // Arrange
        var result = Result.Success<int, string>(21);

        // Act
        var mapped = result.Map(value => value * 2);

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void Map_keeps_the_error_of_a_failure_and_skips_the_projection()
    {
        // Arrange
        var result = Result.Failure<int, string>(Error);
        var projected = false;

        // Act
        var mapped = result.Map(value =>
        {
            projected = true;
            return value * 2;
        });

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal(Error, mapped.Error);
        Assert.False(projected);
    }

    [Fact]
    public void MapError_projects_the_error_of_a_failure()
    {
        // Arrange
        var result = Result.Failure<int, string>(Error);

        // Act
        var mapped = result.MapError(error => error.Length);

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal(Error.Length, mapped.Error);
    }

    [Fact]
    public void MapError_keeps_the_value_of_a_success()
    {
        // Arrange
        var result = Result.Success<int, string>(42);

        // Act
        var mapped = result.MapError(error => error.Length);

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void Bind_chains_onto_a_success()
    {
        // Arrange
        var result = Result.Success<int, string>(21);

        // Act
        var bound = result.Bind(value => Result.Success<string, string>($"#{value}"));

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal("#21", bound.Value);
    }

    [Fact]
    public void Bind_short_circuits_a_failure()
    {
        // Arrange
        var result = Result.Failure<int, string>(Error);
        var chained = false;

        // Act
        var bound = result.Bind(value =>
        {
            chained = true;
            return Result.Success<string, string>($"#{value}");
        });

        // Assert
        Assert.True(bound.IsFailure);
        Assert.Equal(Error, bound.Error);
        Assert.False(chained);
    }

    [Fact]
    public void Bind_to_a_unit_result_discards_the_value()
    {
        // Arrange
        var result = Result.Success<int, string>(21);

        // Act
        var bound = result.Bind(_ => UnitResult.Success<string>());

        // Assert
        Assert.True(bound.IsSuccess);
    }

    [Fact]
    public void Bind_chains_a_unit_result_onto_a_unit_result()
    {
        // Arrange
        var success = UnitResult.Success<string>();
        var failure = UnitResult.Failure(Error);
        var chained = false;

        // Act
        var boundSuccess = success.Bind(() => UnitResult.Failure("second"));
        var boundFailure = failure.Bind(() =>
        {
            chained = true;
            return UnitResult.Success<string>();
        });

        // Assert
        Assert.Equal("second", boundSuccess.Error);
        Assert.Equal(Error, boundFailure.Error);
        Assert.False(chained);
    }

    [Fact]
    public void Ensure_fails_a_success_that_does_not_satisfy_the_predicate()
    {
        // Arrange
        var result = Result.Success<int, string>(-1);

        // Act
        var ensured = result.Ensure(value => value > 0, Error);

        // Assert
        Assert.True(ensured.IsFailure);
        Assert.Equal(Error, ensured.Error);
    }

    [Fact]
    public void Ensure_keeps_a_success_that_satisfies_the_predicate()
    {
        // Arrange
        var result = Result.Success<int, string>(42);

        // Act
        var ensured = result.Ensure(value => value > 0, value => $"{value} is not positive");

        // Assert
        Assert.True(ensured.IsSuccess);
        Assert.Equal(42, ensured.Value);
    }

    [Fact]
    public void Tap_runs_only_for_a_success_and_returns_the_original_result()
    {
        // Arrange
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>(Error);
        var observed = new List<int>();

        // Act
        var tappedSuccess = success.Tap(observed.Add);
        var tappedFailure = failure.Tap(observed.Add);

        // Assert
        Assert.Equal([42], observed);
        Assert.Equal(success, tappedSuccess);
        Assert.Equal(failure, tappedFailure);
    }

    [Fact]
    public void TapError_runs_only_for_a_failure_and_returns_the_original_result()
    {
        // Arrange
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>(Error);
        var observed = new List<string>();

        // Act
        var tappedSuccess = success.TapError(observed.Add);
        var tappedFailure = failure.TapError(observed.Add);

        // Assert
        Assert.Equal([Error], observed);
        Assert.Equal(success, tappedSuccess);
        Assert.Equal(failure, tappedFailure);
    }

    [Fact]
    public void Match_runs_the_branch_matching_the_state()
    {
        // Arrange
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>(Error);

        // Act
        var fromSuccess = success.Match(value => $"value {value}", error => $"error {error}");
        var fromFailure = failure.Match(value => $"value {value}", error => $"error {error}");

        // Assert
        Assert.Equal("value 42", fromSuccess);
        Assert.Equal($"error {Error}", fromFailure);
    }

    [Fact]
    public void Match_runs_the_action_matching_the_state_of_a_unit_result()
    {
        // Arrange
        var failure = UnitResult.Failure(Error);
        var observed = string.Empty;

        // Act
        failure.Match(() => observed = "success", error => observed = error);

        // Assert
        Assert.Equal(Error, observed);
    }

    [Fact]
    public void Extensions_reject_a_missing_delegate()
    {
        // Arrange
        var result = Result.Success<int, string>(42);

        // Act
        var act = () => { _ = result.Map<int, int, string>(null!); };

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }
}
