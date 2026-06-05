using InvestmentTracker.Api.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvestmentTracker.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Office> Offices => Set<Office>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>("OfficeIdSequence", schema: "dbo")
            .StartsAt(111)
            .IncrementsBy(1);

        modelBuilder.Entity<Office>(entity =>
        {
            entity.ToTable("Offices");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).UseIdentityColumn();

            entity.Property(e => e.OfficeId)
                .HasDefaultValueSql("NEXT VALUE FOR dbo.OfficeIdSequence");
            entity.HasIndex(e => e.OfficeId).IsUnique();

            entity.Property(e => e.OfficeName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ManagerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();

            entity.Property(e => e.Phone).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Phone).IsUnique();

            entity.Property(e => e.UserName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();

            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();
        });
    }
}
