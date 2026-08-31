using Microsoft.EntityFrameworkCore;
using Tatami.Domain.Entities;

namespace Tatami.Infrastructure.Persistence;

public class TatamiDbContext : DbContext
{
    public TatamiDbContext(DbContextOptions<TatamiDbContext> options)
        : base(options)
    {
    }

    public DbSet<Academy> Academies => Set<Academy>();
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TatamiDbContext).Assembly);
    }
}
