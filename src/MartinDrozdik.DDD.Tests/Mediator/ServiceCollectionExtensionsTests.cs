using MartinDrozdik.DDD.Mediator;
using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestPipelines;
using MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestRequests;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Tests.Mediator;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task Manual_query_registrations_work_correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(builder =>
        {
            builder.WithQuery<TestPipelineQuery, int, TestPipelineQueryHandler>();
        });

        // Act & Assert
        await RunTestQueryRequests(services);
    }

    [Fact]
    public async Task Manual_query_registrations_with_pipeline_work_correctly()
    {
        // Arrange
        const string pipelineId = "id1";
        var services = new ServiceCollection();
        services.AddSingleton("id1");
        services.AddSingleton<TestQueryPipeline>();
        services.AddMediator(builder =>
        {
            var servicePipelineBuilder = new ServicePipelineBuilder<TestPipelineQuery, int>()
                .Add<TestQueryPipeline>();
            builder.WithQuery<TestPipelineQuery, int, TestPipelineQueryHandler>(servicePipelineBuilder);
        });

        // Act & Assert
        await RunTestQueryRequests(services, pipelineId);
    }

    [Fact]
    public async Task Manual_command_registrations_work_correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(builder =>
        {
            builder.WithCommand<TestPipelineCommand, int, TestPipelineCommandHandler>();
        });

        // Act & Assert
        await RunTestCommandRequests(services);
    }

    [Fact]
    public async Task Manual_command_registrations_with_pipeline_work_correctly()
    {
        // Arrange
        const string pipelineId = "id1";
        var services = new ServiceCollection();
        services.AddSingleton("id1");
        services.AddSingleton<TestCommandPipeline>();
        services.AddMediator(builder =>
        {
            var servicePipelineBuilder = new ServicePipelineBuilder<TestPipelineCommand, int>()
                .Add<TestCommandPipeline>();
            builder.WithCommand<TestPipelineCommand, int, TestPipelineCommandHandler>(servicePipelineBuilder);
        });

        // Act & Assert
        await RunTestCommandRequests(services, pipelineId);
    }

    [Fact]
    public async Task Manual_unit_command_registrations_work_correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(builder =>
        {
            builder.WithCommand<TestPipelineUnitCommand, TestPipelineUnitCommandHandler>();
        });

        // Act & Assert
        await RunTestUnitCommandRequests(services);
    }

    [Fact]
    public async Task Manual_unit_command_registrations_with_pipeline_work_correctly()
    {
        // Arrange
        const string pipelineId = "id1";
        var services = new ServiceCollection();
        services.AddSingleton("id1");
        services.AddSingleton<TestUnitCommandPipeline>();
        services.AddMediator(builder =>
        {
            var servicePipelineBuilder = new ServicePipelineBuilder<TestPipelineUnitCommand>()
                .Add<TestUnitCommandPipeline>();
            builder.WithCommand<TestPipelineUnitCommand, TestPipelineUnitCommandHandler>(servicePipelineBuilder);
        });

        // Act & Assert
        await RunTestUnitCommandRequests(services, pipelineId);
    }

    [Fact]
    public async Task Manual_registrations_work_correctly_together()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(builder =>
        {
            builder.WithQuery<TestPipelineQuery, int, TestPipelineQueryHandler>();
            builder.WithCommand<TestPipelineCommand, int, TestPipelineCommandHandler>();
            builder.WithCommand<TestPipelineUnitCommand, TestPipelineUnitCommandHandler>();
        });

        // Act & Assert
        await RunTestRequests(services);
    }

    [Fact]
    public async Task Manual_registrations_with_pipelines_work_correctly_together()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton("id1");
        services.AddSingleton<TestQueryPipeline>();
        services.AddSingleton<TestCommandPipeline>();
        services.AddSingleton<TestUnitCommandPipeline>();
        services.AddMediator(builder =>
        {
            var serviceQueryPipelineBuilder = new ServicePipelineBuilder<TestPipelineQuery, int>()
                .Add<TestQueryPipeline>();
            builder.WithQuery<TestPipelineQuery, int, TestPipelineQueryHandler>(serviceQueryPipelineBuilder);

            var serviceCommandPipelineBuilder = new ServicePipelineBuilder<TestPipelineCommand, int>()
                .Add<TestCommandPipeline>();
            builder.WithCommand<TestPipelineCommand, int, TestPipelineCommandHandler>(serviceCommandPipelineBuilder);

            var serviceUnitCommandPipelineBuilder = new ServicePipelineBuilder<TestPipelineUnitCommand>()
                .Add<TestUnitCommandPipeline>();
            builder.WithCommand<TestPipelineUnitCommand, TestPipelineUnitCommandHandler>(serviceUnitCommandPipelineBuilder);
        });

        // Act & Assert
        await RunTestRequests(services, "id1");
    }

    private static async Task RunTestQueryRequests(ServiceCollection services, string pipelineId = "")
    {
        // Arrange
        var provider = services.BuildServiceProvider();
        var mediator = new ServiceMediator(provider);
        var query = new TestPipelineQuery(2);
        var hasPipeline = !string.IsNullOrEmpty(pipelineId);
        var expectedCallStack = hasPipeline ? new[] { pipelineId } : [];

        // Act
        var result = await mediator.SendQuery<TestPipelineQuery, int>(query, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => query.AssertCallStack(expectedCallStack),
            () => Assert.Equal(result, hasPipeline ? query.Result + 1 : query.Result));
    }

    private static async Task RunTestCommandRequests(ServiceCollection services, string pipelineId = "")
    {
        // Arrange
        var provider = services.BuildServiceProvider();
        var mediator = new ServiceMediator(provider);
        var command = new TestPipelineCommand(2);
        var hasPipeline = !string.IsNullOrEmpty(pipelineId);
        var expectedCallStack = hasPipeline ? new[] { pipelineId } : [];

        // Act
        var result = await mediator.SendCommand<TestPipelineCommand, int>(command, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => command.AssertCallStack(expectedCallStack),
            () => Assert.Equal(result, hasPipeline ? command.Result + 1 : command.Result));
    }

    private static async Task RunTestUnitCommandRequests(ServiceCollection services, string pipelineId = "")
    {
        // Arrange
        var provider = services.BuildServiceProvider();
        var mediator = new ServiceMediator(provider);
        var command = new TestPipelineUnitCommand();
        var hasPipeline = !string.IsNullOrEmpty(pipelineId);
        var expectedCallStack = hasPipeline ? new[] { pipelineId } : [];

        // Act
        await mediator.SendCommand(command, CancellationToken.None);

        // Assert
        command.AssertCallStack(expectedCallStack);
    }

    private static async Task RunTestRequests(ServiceCollection services, string pipelineId = "")
    {
        await RunTestQueryRequests(services, pipelineId);
        await RunTestCommandRequests(services, pipelineId);
        await RunTestUnitCommandRequests(services, pipelineId);
    }
}
