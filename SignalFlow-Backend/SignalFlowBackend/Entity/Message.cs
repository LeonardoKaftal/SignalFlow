using System.ComponentModel.DataAnnotations;

namespace SignalFlowBackend.Entity;

public class Message
{
   [Key]
   public Guid MessageId { get; set; } 
   public Guid ConversationId { get; set; }
   public ChatConversation Conversation { get; set; }
   // ConversationParticipantGuid, might be null because the sender could not be part of the conversation anymore
   public Guid? SenderId { get; set; }
   public ConversationParticipant? Sender { get; set; }
   public required DateTime SentTime { get; set; }
   public required string Content { get; set; }
}