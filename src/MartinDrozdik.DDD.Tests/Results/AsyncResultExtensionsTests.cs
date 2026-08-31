using MartinDrozdik.DDD.Results;

namespace MartinDrozdik.DDD.Tests.Results;

public class AsyncResultExtensionsTests
{
    private const string Error = "boom";

    [Fact]
    public async Task MapAsync_projects_the_value_of_a_success()
    {
        // Arrange
        var result = Result.Success<int, string>(21);

        // Act
        var mapped = await result.MapAsync(value => Task.FromResult(value * 2));

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public async Task MapAsync_short_circuits_a_failed_task_of_a_result()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Failure<int, string>(Error));
        var projected = false;

        // Act
        var mapped = await resultTask.MapAsync(value =>
        {
            projected = true;
            return Task.FromResult(value * 2);
        });

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal(Error, mapped.Error);
        Assert.False(projected);
    }

    [Fact]
    public async Task BindAsync_chains_onto_a_task_of_a_success()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Success<int, string>(21));

        // Act
        var bound = await resultTask.BindAsync(value => Task.FromResult(Result.Success<string, string>($"#{value}")));

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal("#21", bound.Value);
    }

    [Fact]
    public async Task BindAsync_short_circuits_a_failed_unit_result()
    {
        // Arrange
        var result = UnitResult.Failure(Error);
        var chained = false;

        // Act
        var bound = await result.BindAsync(() =>
        {
            chained = true;
            return Task.FromResult(UnitResult.Success<string>());
        });

        // Assert
        Assert.True(bound.IsFailure);
        Assert.False(chained);
    }

    [Fact]
    public async Task TapAsync_runs_only_for_a_success_and_returns_the_original_result()
    {
        // Arrange
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>(Error);
        var observed = new List<int>();

        // Act
        var tappedSuccess = await success.TapAsync(value =>
        {
            observed.Add(value);
            return Task.CompletedTask;
        });
        var tappedFailure = await failure.TapAsync(value =>
        {
            observed.Add(value);
            return Task.CompletedTask;
        });

        // Assert
        Assert.Equal([42], observed);
        Assert.Equal(success, tappedSuccess);
        Assert.Equal(failure, tappedFailure);
    }

    [Fact]
    public async Task TapErrorAsync_runs_only_for_a_failed_unit_result()
    {
        // Arrange
        var resultTask = Task.FromResult(UnitResult.Failure(Error));
        var observed = new List<string>();

        // Act
        var tapped = await resultTask.TapErrorAsync(error =>
        {
            observed.Add(error);
            return Task.CompletedTask;
        });

        // Assert
        Assert.Equal([Error], observed);
        Assert.True(tapped.IsFailure);
    }

    [Fact]
    public async Task MatchAsync_runs_the_branch_matching_the_state()
    {
        // Arrange
        var successTask = Task.FromResult(Result.Success<int, string>(42));
        var failureTask = Task.FromResult(Result.Failure<int, string>(Error));

        // Act
        var fromSuccess = await successTask.MatchAsync(
            value => Task.FromResult($"value {value}"),
            error => Task.FromResult($"error {error}"));
        var fromFailure = await failureTask.MatchAsync(
            value => Task.FromResult($"value {value}"),
            error => Task.FromResult($"error {error}"));

        // Assert
        Assert.Equal("value 42", fromSuccess);
        Assert.Equal($"error {Error}", fromFailure);
    }

    [Fact]
    public async Task MatchAsync_runs_the_branch_matching_the_state_of_a_unit_result()
    {
        // Arrange
        var result = UnitResult.Success<string>();

        // Act
        var matched = await result.MatchAsync(
            () => Task.FromResult("success"),
            error => Task.FromResult($"error {error}"));

        // Assert
        Assert.Equal("success", matched);
    }
}
