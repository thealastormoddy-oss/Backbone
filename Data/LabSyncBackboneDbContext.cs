using Microsoft.EntityFrameworkCore;

namespace LabSyncBackbone.Data
{
    public class LabSyncBackboneDbContext : DbContext
    {
        public DbSet<CachedCase> CachedCases { get; set; } = null!;
        public DbSet<FailedRequest> FailedRequests { get; set; } = null!;

        public LabSyncBackboneDbContext(DbContextOptions<LabSyncBackboneDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // No two rows can have the same CaseId — prevents duplicate entries
            modelBuilder.Entity<CachedCase>().HasIndex(c => c.CaseId).IsUnique();

            // Store FailedRequestStatus enum as its string name ("Pending", "Succeeded", "Exhausted")
            // so the table stays human-readable in pgAdmin/TablePlus
            modelBuilder.Entity<FailedRequest>()
                .Property(f => f.Status)
                .HasConversion<string>();
        }
    }
}
