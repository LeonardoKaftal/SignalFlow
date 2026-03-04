using System.ComponentModel.DataAnnotations;

namespace SignalFlowBackend.Entity;

public class ConversationParticipant
{
    [Key] 
    public Guid ConversationParticipantId { get; set; }
        
    public required Guid UserId { get; set; }
    public required User User { get; set; }
    
    public required Guid ConversationId { get; set; }
    public required ChatConversation ChatConversation { get; set; }
    
    public required DateTime LastAccess { get; set; }
}