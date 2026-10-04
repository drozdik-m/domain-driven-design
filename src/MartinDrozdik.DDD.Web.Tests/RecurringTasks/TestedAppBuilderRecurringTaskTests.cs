using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.RecurringTasks;
using MartinDrozdik.DDD.Web.Blobs.Sweepers;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

/// <summary>
/// Verifies the recurring task support of the testing package:
/// <see cref="TestedAppBuilder{TProgram}.Build"/> removing the loops by default, <see cref="TestedAppBuilder{TProgram}.WithRecurringTasks"/>,
/// <see cref="TestedAppBuilder{TProgram}.WithoutHostedService{THostedService}"/> and <see cref="RecurringTaskTestExtensions"/>.
/// </summary>
public class TestedAppBuilderRecurringTaskTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Build_removes_every_recurring_task_loop_by_default()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();

        // Act
        var hostedServices = app.Services.GetServices<IHostedService>();

        // Assert
        Assert.DoesNotContain(hostedServices, IsRecurringTaskLoop);
    }

    [Fact]
    public void WithRecurringTasks_keeps_every_recurring_task_loop()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithRecurringTasks()
            .Build();

        // Act
        var hostedServices = app.Services.GetServices<IHostedService>().ToList();

        // Assert
        Assert.Single(hostedServices.OfType<RecurringTaskHost<TestRecurringTask>>());
        Assert.Single(hostedServices.OfType<RecurringTaskHost<OutboxDispatchRecurringTask>>());
        Assert.Single(hostedServices.OfType<RecurringTaskHost<BlobSweepRecurringTask>>());
    }

    [Fact]
    public void WithoutHostedService_removes_only_that_hosted_service()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper)
            .WithRecurringTasks()
            .WithServices(services => services.AddHostedService<UnwantedWorker>())
            .WithoutHostedService<UnwantedWorker>()
            .Build();

        // Act
        var hostedServices = app.Services.GetServices<IHostedService>().ToList();

        // Assert
        Assert.Empty(hostedServices.OfType<UnwantedWorker>());
        Assert.Single(hostedServices.OfType<RecurringTaskHost<TestRecurringTask>>());
    }

    [Fact]
    public async Task RunRecurringTaskAsync_runs_the_task_although_its_loop_was_removed()
    {
        // Arrange
        using var app = new TestedWebAppBuilder(testOutputHelper).Build();
        var runs = app.Services.GetRequiredService<TestRecurringTaskRuns>();

        // Act
        await app.RunRecurringTaskAsync<TestRecurringTask>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, runs.Count);
    }

    [Fact]
    public async Task RunRecurringTaskAsync_rethrows_what_the_task_throws_instead_of_logging_it_like_the_loop()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ThrowingTask>();
        await using var provider = services.BuildServiceProvider();

        // Act
        var exception = await Record.ExceptionAsync(
            () => provider.RunRecurringTaskAsync<ThrowingTask>(TestContext.Current.CancellationToken));

        // Assert
        var thrown = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(ThrowingTask.Message, thrown.Message);
    }

    private static bool IsRecurringTaskLoop(IHostedService service)
    {
        var type = service.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RecurringTaskHost<>);
    }

    private sealed class ThrowingTask : IRecurringTask
    {
        public const string Message = "The task failed.";

        public Task RunAsync(CancellationToken cancellationToken) => throw new InvalidOperationException(Message);
    }

    private sealed class UnwantedWorker : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}
