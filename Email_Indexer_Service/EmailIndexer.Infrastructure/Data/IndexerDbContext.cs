using Microsoft.EntityFrameworkCore;
using EmailIndexer.Domain.Models;

namespace EmailIndexer.Infrastructure.Data
{
    public class IndexerDbContext : DbContext
    {
        public DbSet<Email> Emails { get; set; }

        public IndexerDbContext(DbContextOptions<IndexerDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Database=emailindexer;Username=postgres;Password=yourpassword",
                    b => b.MigrationsAssembly("EmailIndexer.Infrastructure")); // Explicitly set the migrations assembly
            }
        }
    }
}