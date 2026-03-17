using System.ComponentModel.DataAnnotations;

namespace SignalFlowBackend.Entity;

public class ChatConversation
{
    [Key]
    public Guid ConversationId { get; set; }
    public required string Name { get; set; }
    public required bool IsGlobal { get; set; }
    public required DateTime CreatedAt { get; set; }
    public ICollection<ConversationParticipant> Participants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}