using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tatami.Domain.Entities;
using Tatami.Domain.Enums;

namespace Tatami.Infrastructure.Persistence.Configurations;

public class FinancialConfiguration : IEntityTypeConfiguration<Financial>
{
    public void Configure(EntityTypeBuilder<Financial> builder)
    {
        builder.ToTable("financials", table =>
        {
            table.HasCheckConstraint("CK_financials_ReferenceMonth", "EXTRACT(DAY FROM \"ReferenceMonth\") = 1");
            table.HasCheckConstraint("CK_financials_Amount", "\"Amount\" > 0");
            table.HasCheckConstraint("CK_financials_Status", "\"Status\" IN ('pending', 'paid', 'overdue', 'aguardando_confirmacao')");
        });
        builder.HasKey(financial => financial.Id);
        builder.Property(financial => financial.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(financial => financial.Status).HasMaxLength(30).HasConversion(
            status => status == FinancialStatus.AguardandoConfirmacao ? "aguardando_confirmacao" : status.ToString().ToLowerInvariant(),
            value => value == "aguardando_confirmacao" ? FinancialStatus.AguardandoConfirmacao : Enum.Parse<FinancialStatus>(value, true));
        builder.HasIndex(financial => new { financial.AcademyId, financial.Status, financial.DueDate });
        builder.HasIndex(financial => new { financial.StudentId, financial.DueDate });
        builder.HasIndex(financial => new { financial.StudentId, financial.ReferenceMonth }).IsUnique();
        builder.HasOne<Academy>().WithMany().HasForeignKey(financial => financial.AcademyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(financial => financial.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}