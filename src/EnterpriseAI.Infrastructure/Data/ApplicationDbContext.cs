using EnterpriseAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAI.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Title)
                .HasMaxLength(500);

            entity.HasMany(x => x.Messages)
                .WithOne(x => x.Conversation)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Role)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Content)
                .IsRequired();

            entity.HasIndex(x =>
                new
                {
                    x.ConversationId,
                    x.CreatedAt
                });
        });
    }
}