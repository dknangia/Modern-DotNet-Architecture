using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options)
        {
            
        }

        public DbSet<WorkTask> WorkTasks { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkTask>(e =>
            {
                e.HasKey(w => w.Id);
                e.Property(w => w.Name).IsRequired().HasMaxLength(200);
                e.Property(w => w.Status).HasConversion<int>(); //Store enum as int
            });
        }
    }
}
