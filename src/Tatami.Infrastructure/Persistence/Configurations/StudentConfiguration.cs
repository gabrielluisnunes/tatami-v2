using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tatami.Domain.Entities;

namespace Tatami.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.HasKey(student => student.Id);

        builder.Property(student => student.AcademyId)
            .IsRequired();

        builder.Property(student => student.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(student => student.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(student => student.Email);

        builder.Property(student => student.CreatedAt)
            .IsRequired();

        builder.Property(student => student.UpdatedAt)
            .IsRequired();

        builder.HasOne<Academy>()
            .WithMany()
            .HasForeignKey(student => student.AcademyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
