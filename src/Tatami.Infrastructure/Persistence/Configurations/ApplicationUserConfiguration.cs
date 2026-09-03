using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tatami.Domain.Entities;
using Tatami.Infrastructure.Identity;

namespace Tatami.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .IsRequired();

        builder.HasIndex(user => user.AcademyId);

        builder.HasOne<Academy>()
            .WithMany()
            .HasForeignKey(user => user.AcademyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
