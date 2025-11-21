using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Models.Identities.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MartinDrozdik.DDD.Demo.Context.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Person");
        builder.HasKey(p => p.Id)
            .HasName("PersonId");
        builder.Property(p => p.Id)
            .HasIdentityConvertor(IdentityConverter.CreateGuid(key => new PersonId(key)));
        builder.Property(p => p.FullName)
            .HasMaxLength(Person.FullNameMaxLength)
            .IsRequired();
        builder.Property(p => p.DateOfBirth)
            .IsRequired();
    }
}
