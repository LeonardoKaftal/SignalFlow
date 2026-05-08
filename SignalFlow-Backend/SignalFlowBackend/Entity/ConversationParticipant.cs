using System.ComponentModel.DataAnnotations;
using SignalFlowBackend.Role;

namespace SignalFlowBackend.Entity;

public class ConversationParticipant
{
    [Key] 
    public Guid ConversationParticipantId { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public Guid ConversationId { get; set; }
    public ChatConversation ChatConversation { get; set; }
    public ConversationParticipantRole Role { get; set; }
    public Guid? LastMessageRead { get; set; }
}