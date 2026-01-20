using FluentValidation;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Mediator.Commands;
using MartinDrozdik.DDD.Mediator.Pipelines.Validations;
using MartinDrozdik.DDD.Mediator.Queries;

namespace MartinDrozdik.DDD.Tests.Mediator.Pipelines.Validations;

public class ValidationPipelineTests
{
    [Fact]
    public async Task Unit_command_pipeline_passes_when_request_is_valid()
    {
        // Arrange
        var pipeline = new ValidationPipeline<TestValidatedUnitCommand>();
        var command = new TestValidatedUnitCommand { Value = 5 };
        var nextCalled = false;

        Task Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await pipeline.HandleAsync(command, Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Unit_command_pipeline_throws_when_request_is_invalid()
    {
        // Arrange
        var pipeline = new ValidationPipeline<TestValidatedUnitCommand>();
        var command = new TestValidatedUnitCommand { Value = -1 };
        var nextCalled = false;
        Task Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => pipeline.HandleAsync(command, Next, CancellationToken.None));
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Unit_command_pipeline_passes_when_request_is_not_validated()
    {
        // Arrange
        var pipeline = new ValidationPipeline<RegularUnitCommand>();
        var command = new RegularUnitCommand() { Value = 5 };
        var nextCalled = false;
        Task Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await pipeline.HandleAsync(command, Next, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => Assert.True(nextCalled));
    }

    [Fact]
    public async Task Command_pipeline_passes_when_request_is_valid()
    {
        // Arrange
        var pipeline = new ValidationPipeline<TestValidatedCommand>();
        var command = new TestValidatedCommand { Value = 5 };
        var nextCalled = false;

        Task Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await pipeline.HandleAsync(command, Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Command_pipeline_throws_when_request_is_invalid()
    {
        // Arrange
        var pipeline = new ValidationPipeline<TestValidatedCommand>();
        var command = new TestValidatedCommand { Value = -1 };
        var nextCalled = false;
        Task Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => pipeline.HandleAsync(command, Next, CancellationToken.None));
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Command_pipeline_passes_when_request_is_not_validated()
    {
        // Arrange
        var pipeline = new ValidationPipeline<RegularCommand, int>();
        var command = new RegularCommand() { Value = 5 };
        var nextCalled = false;
        Task<int> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult(command.Value);
        }

        // Act
        var result = await pipeline.HandleAsync(command, Next, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => Assert.True(nextCalled),
            () => Assert.Equal(command.Value, result));
    }

    [Fact]
    public async Task Query_pipeline_passes_when_request_is_valid()
    {
        // Arrange
        var pipeline = new ValidationPipeline<TestValidatedQuery>();
        var query = new TestValidatedQuery { Value = 5 };
        var nextCalled = false;

        Task Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await pipeline.HandleAsync(query, Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Query_pipeline_throws_when_request_is_invalid()
    {
        // Arrange
        var pipeline = new ValidationPipeline<TestValidatedQuery>();
        var query = new TestValidatedQuery { Value = -1 };
        var nextCalled = false;
        Task Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleValidationException>(
            () => pipeline.HandleAsync(query, Next, CancellationToken.None));
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Query_pipeline_passes_when_request_is_not_validated()
    {
        // Arrange
        var pipeline = new ValidationPipeline<RegularQuery, int>();
        var query = new RegularQuery() { Value = 5 };
        var nextCalled = false;
        Task<int> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult(query.Value);
        }

        // Act
        var result = await pipeline.HandleAsync(query, Next, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => Assert.True(nextCalled),
            () => Assert.Equal(query.Value, result));
    }

    public class TestValidatedUnitCommand : ICommand, IValidatedMessage<TestValidatedUnitCommand>
    {
        public AbstractValidator<TestValidatedUnitCommand> Validator => new TestValidatedUnitCommandValidator();

        public int Value { get; set; }

        public class TestValidatedUnitCommandValidator : AbstractValidator<TestValidatedUnitCommand>
        {
            public TestValidatedUnitCommandValidator()
            {
                RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
            }
        }
    }

    public class RegularUnitCommand : ICommand
    {
        public int Value { get; set; }
    }

    public class TestValidatedCommand : ICommand<int>, IValidatedMessage<TestValidatedCommand>
    {
        public AbstractValidator<TestValidatedCommand> Validator => new TestValidatedCommandValidator();

        public int Value { get; set; }

        public class TestValidatedCommandValidator : AbstractValidator<TestValidatedCommand>
        {
            public TestValidatedCommandValidator()
            {
                RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
            }
        }
    }

    public class RegularCommand : ICommand<int>
    {
        public int Value { get; set; }
    }

    public class TestValidatedQuery : IQuery<int>, IValidatedMessage<TestValidatedQuery>
    {
        public AbstractValidator<TestValidatedQuery> Validator => new TestValidatedQueryValidator();

        public int Value { get; set; }

        public class TestValidatedQueryValidator : AbstractValidator<TestValidatedQuery>
        {
            public TestValidatedQueryValidator()
            {
                RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
            }
        }
    }

    public class RegularQuery : IQuery<int>
    {
        public int Value { get; set; }
    }
}
