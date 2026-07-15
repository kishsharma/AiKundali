using KundaliEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace KundaliEngine.Infrastructure.Data;

public class KundaliDbContext : DbContext
{
    public KundaliDbContext(DbContextOptions<KundaliDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<AstrologerBrandingSetting> AstrologerBrandingSettings => Set<AstrologerBrandingSetting>();

    public DbSet<Kundali> Kundalis => Set<Kundali>();

    public DbSet<KundaliReport> KundaliReports => Set<KundaliReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KundaliDbContext).Assembly);
    }
}

