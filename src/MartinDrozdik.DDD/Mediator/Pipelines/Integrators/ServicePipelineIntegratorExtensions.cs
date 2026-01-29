namespace MartinDrozdik.DDD.Mediator.Pipelines.Integrators;

/// <summary>
/// Extension methods for <see cref="IServicePipelineIntegrator"/>.
/// </summary>
public static class ServicePipelineIntegratorExtensions
{
    /// <summary>
    /// Merges additional <see cref="IServicePipelineIntegrator"/> into the <paramref name="currentIntegrator"/>.
    /// </summary>
    /// <param name="currentIntegrator">The integrator to extend.</param>
    /// <param name="newIntegrator">Additional integrator.</param>
    /// <returns>New merged integrator.</returns>
    public static MergedPipelineIntegrator Merge(this IServicePipelineIntegrator currentIntegrator, IServicePipelineIntegrator newIntegrator)
    {
        if (currentIntegrator is MergedPipelineIntegrator mergedIntegrator)
        {
            return mergedIntegrator.Merge(newIntegrator);
        }

        return new MergedPipelineIntegrator(currentIntegrator, newIntegrator);
    }

    /// <summary>
    /// Merges additional <see cref="IServicePipelineIntegrator"/> into the <paramref name="currentIntegrator"/>.
    /// </summary>
    /// <param name="currentIntegrator">The integrator to extend.</param>
    /// <typeparam name="TIntegrator">Type of the additional integrator.</typeparam>
    /// <returns>New merged integrator.</returns>
    public static MergedPipelineIntegrator Merge<TIntegrator>(this IServicePipelineIntegrator currentIntegrator)
        where TIntegrator : IServicePipelineIntegrator, new()
    {
        var newIntegrator = new TIntegrator();
        return currentIntegrator.Merge(newIntegrator);
    }
}
