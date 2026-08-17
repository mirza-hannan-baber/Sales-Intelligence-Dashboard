using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Deal> Deals => Set<Deal>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Agent> Agents => Set<Agent>();
        public DbSet<PredictionLog> PredictionLogs => Set<PredictionLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Deal>().HasIndex(d => d.OpportunityId);
            builder.Entity<Account>().HasIndex(a => a.AccountId);
            builder.Entity<Agent>().HasIndex(a => a.Name);
        }
    }
}
