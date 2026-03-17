using SignalFlowBackend.Dto;

namespace SignalFlowBackend.Service;

public interface IMessageService
{
    public Task<IEnumerable<MessageDto>?> GetAllMessagesByConversationId(Guid conversationId);
    public Task<IEnumerable<MessageDto>?> GetAllMessagesByConversationIdAndConversationParticipantId(Guid conversationId, Guid ConversationParticipantId);
    public Task<MessageDto?> GetMessageById(Guid messageId);
    public Task<MessageDto?> SaveMessage(MessageDto toSave);
    public Task<bool> DeleteMessage(Guid messageId);
    public Task<MessageDto?> UpdateMessage(Guid messageId, string newContent);
    public Task<MessageDto?> GetLatestMessageByConversationId(Guid conversationId);
    public Task<IEnumerable<MessageDto>?> GetAllMessagesByUserId(Guid userId);
}