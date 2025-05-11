using Microsoft.EntityFrameworkCore;
using MultiCopierWPF.Models;

namespace MultiCopierWPF.Data;

public class BackupDbContext : DbContext
{
    public DbSet<BackupEntry> BackupEntries { get; set; }

    public BackupDbContext(DbContextOptions<BackupDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Optional: Fluent config
        //modelBuilder.Entity<BackupEntry>().ToTable("BackupEntries");
    }
}
