namespace MartinDrozdik.DDD.Disposing;

/// <summary>
/// An action invoked when the object is disposed.
/// </summary>
/// <remarks>
/// Useful for creating cleanup actions.
/// </remarks>
/// <param name="disposeAction">The action to execute at dispose time.</param>
public class DisposableAction(Action disposeAction) : IDisposable
{
    private bool _isDisposed;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()" />
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                disposeAction();
            }

            _isDisposed = true;
        }
    }
}
