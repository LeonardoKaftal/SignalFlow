using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<ConversationParticipant> Participants { get; set; }
    public DbSet<ChatConversation> Conversations { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ConversationParticipant>()
            .HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConversationParticipant>()
            .HasOne(cp => cp.ChatConversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConversationParticipant>()
            .HasIndex(cp => new { cp.UserId, cp.ConversationId })
            .IsUnique();

        modelBuilder.Entity<ConversationParticipant>()
            .HasIndex(cp => cp.ConversationId);

        modelBuilder.Entity<ChatConversation>()
            .Property(c => c.Name)
            .IsRequired();

        modelBuilder.Entity<ChatConversation>()
            .HasIndex(c => c.Name);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.ConversationId, m.SentTime });

        modelBuilder.Entity<Message>()
            .HasIndex(m => m.SenderId);
    }
};