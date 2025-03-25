namespace MartinDrozdik.DDD.Models.Identities;

/// <summary>
/// Complement interface for <see cref="IIdentity{TSelf, TValue}"/> that provides an implicit casting from <typeparamref name="TValue"/> to the strongly typed ID.
/// </summary>
/// <typeparam name="TSelf">Self-referencing generic type.</typeparam>
/// <typeparam name="TValue">Actual value of the ID.</typeparam>
public interface IWithImplicitIdentity<TSelf, in TValue>
    where TSelf : IWithImplicitIdentity<TSelf, TValue>
    where TValue : notnull
{
    /// <summary>
    /// An implicit casting from <typeparamref name="TValue"/> to the strongly typed ID.
    /// </summary>
    /// <remarks>
    /// This saves a lot of husle when working with IDs, especially in tests.
    /// </remarks>
    /// <param name="id">The <i>raw</i> id to be converted.</param>
    static abstract implicit operator TSelf(TValue id);
}
