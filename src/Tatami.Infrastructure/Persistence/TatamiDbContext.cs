using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tatami.Domain.Entities;
using Tatami.Infrastructure.Identity;

namespace Tatami.Infrastructure.Persistence;

public class TatamiDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public TatamiDbContext(DbContextOptions<TatamiDbContext> options)
        : base(options)
    {
    }

    public DbSet<Academy> Academies => Set<Academy>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TatamiDbContext).Assembly);

        // Tabelas Identity em snake_case (padrão Postgres)
        RenameIdentityTables(modelBuilder);
    }

    private static void RenameIdentityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>().ToTable("users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }
}
