using System.ComponentModel.DataAnnotations;

namespace SignalFlowBackend.Entity;

public class Message
{
   [Key]
   public Guid MessageId { get; set; } 
   public required Guid ConversationId { get; set; }
   public required ChatConversation Conversation { get; set; }
   public required Guid SenderId { get; set; }
   public required ConversationParticipant Sender { get; set; }
   public required DateTime SentTime { get; set; }
   public required string Content { get; set; }
}