using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tatami.Domain.Entities;
using Tatami.Infrastructure.Identity;

namespace Tatami.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.HasKey(student => student.Id);

        builder.Property(student => student.AcademyId)
            .IsRequired();

        builder.Property(student => student.UserId)
            .IsRequired();

        builder.Property(student => student.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(student => student.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(student => student.Phone)
            .HasMaxLength(30);

        builder.Property(student => student.EmergencyPhone)
            .HasMaxLength(30);

        builder.Property(student => student.Cep)
            .HasMaxLength(12);

        builder.Property(student => student.Address)
            .HasMaxLength(300);

        builder.Property(student => student.Neighborhood)
            .HasMaxLength(120);

        builder.Property(student => student.City)
            .HasMaxLength(120);

        builder.Property(student => student.State)
            .HasMaxLength(2);

        builder.Property(student => student.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(student => student.FaceDescriptor)
            .HasColumnType("jsonb");

        builder.Property(student => student.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(student => student.CreatedAt)
            .IsRequired();

        builder.Property(student => student.UpdatedAt)
            .IsRequired();

        builder.HasIndex(student => student.UserId)
            .IsUnique();

        builder.HasIndex(student => new { student.AcademyId, student.Email })
            .IsUnique();

        builder.HasIndex(student => new { student.AcademyId, student.IsActive });

        builder.HasOne<Academy>()
            .WithMany()
            .HasForeignKey(student => student.AcademyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(student => student.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(student => student.Sports)
            .WithOne(sport => sport.Student)
            .HasForeignKey(sport => sport.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
