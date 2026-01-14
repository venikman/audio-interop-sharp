using Microsoft.EntityFrameworkCore;

namespace AudioSharp.App.Data;

public sealed class ConcernsDbContext : DbContext
{
    public ConcernsDbContext(DbContextOptions<ConcernsDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConcernRecord> ConcernRecords => Set<ConcernRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConcernRecord>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Transcript).IsRequired();
            entity.Property(record => record.ConcernsJson).IsRequired();
            entity.Property(record => record.FhirBundleJson).IsRequired();
            entity.Property(record => record.CreatedAtUtc).IsRequired();
        });
    }
}
