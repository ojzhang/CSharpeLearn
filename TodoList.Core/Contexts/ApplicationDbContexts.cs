using System;
using System.Linq;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoList.Core.Models;

namespace TodoList.Core.Contexts
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            ApplyMigrations(this);
        }
        public DbSet<TodoItem> TodoItem { get; set; }
        public DbSet<TodoItemFile> TodoItemFile { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<TodoItem>().ToTable("TodoItem");
            builder.Entity<TodoItemFile>().ToTable("TodoItemFile");
            builder.Entity<TodoItem>()
                .Property<DateTime?>("AddedDateTime")
                .HasColumnName("Added")
                .IsRequired(false);
            builder.Entity<TodoItem>()
                .Property(e => e.Tags)
                .HasConversion(v => string.Join(",", v), v => v.Split(",", StringSplitOptions.RemoveEmptyEntries));
        }

        public void ApplyMigrations(ApplicationDbContext context)
        {
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
        }
    }
}