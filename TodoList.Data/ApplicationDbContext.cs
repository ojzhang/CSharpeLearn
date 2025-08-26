using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoList.Core.Models;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<TodoItemFile> TodoItemFiles { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // 配置模型关系
        builder.Entity<TodoItem>()
            .HasOne<TodoItemFile>()
            .WithOne()
            .HasForeignKey<TodoItemFile>(f => f.TodoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}