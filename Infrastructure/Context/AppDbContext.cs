using Domain.Entities;
using EntitiesConfigurations;
using EntityFrameworkCore.Triggers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class AppDbContext : IdentityDbContext<User, Role, int,
                            IdentityUserClaim<int>, UserRole,
                            IdentityUserLogin<int>, IdentityRoleClaim<int>,
                            IdentityUserToken<int>>
    {


        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserRoleConfig).Assembly);

            modelBuilder.Ignore<IdentityUserToken<int>>();

            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");

            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");

            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");



        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return this.SaveChangesWithTriggersAsync(base.SaveChangesAsync, acceptAllChangesOnSuccess: true, cancellationToken);
        }
    }
}
