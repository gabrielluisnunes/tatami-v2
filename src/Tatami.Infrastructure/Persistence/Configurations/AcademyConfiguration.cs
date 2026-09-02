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

        builder.Property(academy => academy.OwnerId)
            .IsRequired();

        builder.Property(academy => academy.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(academy => academy.Sport)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(academy => academy.MonthlyPrice)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(academy => academy.SubscriptionStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(academy => academy.CreatedAt)
            .IsRequired();

        builder.Property(academy => academy.UpdatedAt)
            .IsRequired();

        builder.HasIndex(academy => academy.OwnerId)
            .IsUnique();
    }
}
