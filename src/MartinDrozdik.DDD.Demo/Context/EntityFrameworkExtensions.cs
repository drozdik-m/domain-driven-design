using MartinDrozdik.DDD.Models.Identities;
using MartinDrozdik.DDD.Models.Identities.Converters;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MartinDrozdik.DDD.Demo.Context;

public static class EntityFrameworkExtensions
{
    public static PropertyBuilder<TIdentity> HasIdentityConvertor<TIdentity, TKey>(this PropertyBuilder<TIdentity> builder, IdentityConverter<TIdentity, TKey> converter)
        where TIdentity : Identity<TIdentity, TKey>
        where TKey : notnull
    {
        return builder.HasConversion(converter.ToKeyExpression, converter.FromKeyExpression);
    }
}
