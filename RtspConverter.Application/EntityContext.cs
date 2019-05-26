using Microsoft.EntityFrameworkCore;
using RtspConverter.Application.Models;
using System.Reflection;

namespace RtspConverter.Application
{
    public class EntityContext : DbContext
    {
        public EntityContext(DbContextOptions<EntityContext> contextOptions)
            : base(contextOptions)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetAssembly(this.GetType()));
        }

        public DbSet<Channel> Channels => Set<Channel>();
    }
}
