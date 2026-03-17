using System.ComponentModel.DataAnnotations;

namespace SignalFlowBackend.Entity;

public class ConversationParticipant
{
    [Key] 
    public Guid ConversationParticipantId { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public Guid ConversationId { get; set; }
    public ChatConversation ChatConversation { get; set; }
    
    public required DateTime LastAccess { get; set; }
}