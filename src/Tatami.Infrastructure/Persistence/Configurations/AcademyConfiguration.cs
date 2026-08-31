using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tatami.Domain.Entities;

namespace Tatami.Infrastructure.Persistence.Configurations;

public class AcademyConfiguration : IEntityTypeConfiguration<Academy>
{
    public void Configure(EntityTypeBuilder<Academy> builder)
    {
        builder.ToTable("academies");

        builder.HasKey(academy => academy.Id);

        builder.Property(academy => academy.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(academy => academy.CreatedAt)
            .IsRequired();

        builder.Property(academy => academy.UpdatedAt)
            .IsRequired();
    }
}
