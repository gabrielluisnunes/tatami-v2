using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tatami.Domain.Entities;

namespace Tatami.Infrastructure.Persistence.Configurations;

public class StudentSportConfiguration : IEntityTypeConfiguration<StudentSport>
{
    public void Configure(EntityTypeBuilder<StudentSport> builder)
    {
        builder.ToTable("student_sports");

        builder.HasKey(sport => sport.Id);

        builder.Property(sport => sport.StudentId)
            .IsRequired();

        builder.Property(sport => sport.AcademyId)
            .IsRequired();

        builder.Property(sport => sport.Sport)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(sport => sport.Belt)
            .HasMaxLength(50);

        builder.Property(sport => sport.Degree)
            .IsRequired();

        builder.Property(sport => sport.BeltUpdatedAt)
            .IsRequired();

        builder.Property(sport => sport.CreatedAt)
            .IsRequired();

        builder.Property(sport => sport.UpdatedAt)
            .IsRequired();

        builder.HasIndex(sport => new { sport.StudentId, sport.Sport })
            .IsUnique();

        builder.HasOne<Academy>()
            .WithMany()
            .HasForeignKey(sport => sport.AcademyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
