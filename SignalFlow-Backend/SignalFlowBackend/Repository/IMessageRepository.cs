using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public interface IMessageRepository
{
    Task<Message?> GetMessageEntityById(Guid messageId);
    Task<MessageDto?> GetMessageById(Guid messageId);
    Task<IEnumerable<MessageDto>> GetAllMessagesByConversationId(Guid conversationId);
    Task<IEnumerable<MessageDto>> GetAllMessagesByConversationIdAndUserId(Guid conversationId, Guid conversationParticipantId);
    Task<IEnumerable<MessageDto>> GetAllMessagesByUserId(Guid userId);
    Task<MessageDto?> Save(Message message);
    Task<bool> Delete(Guid messageId);
    Task<MessageDto?> Update(Guid messageId, string newContent);
    Task<MessageDto?> GetLatestMessageByConversationId(Guid conversationId);
}