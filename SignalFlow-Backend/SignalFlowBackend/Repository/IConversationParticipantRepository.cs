using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public interface IConversationParticipantRepository
{
    Task<ConversationParticipant?> GetParticipantEntityByIdAsync(Guid conversationParticipantId);
    Task<ConversationParticipant?> GetParticipantEntityByUserIdAndConversationIdAsync(Guid userId, Guid conversationId);
    Task<ConversationParticipantDto?> GetParticipantByIdAsync(Guid conversationParticipantId);
    Task<ConversationParticipantDto?> GetParticipantByUserIdAndConversationIdAsync(Guid userId, Guid conversationId);
    Task<IEnumerable<ConversationParticipant>> GetAllParticipantsEntityByConversationIdAsync(Guid conversationId);
    Task<IEnumerable<ConversationParticipantDto>> GetAllParticipantsByConversationIdAsync(Guid conversationId);
    Task<List<ConversationParticipantDto>?> GetAllAdminsByConversationIdAsync(Guid conversationId);
    Task<int> GetNumberOfParticipantsByConversationIdAsync(Guid conversationId);
    Task<ConversationParticipantDto> SaveAsync(ConversationParticipant participant);
    Task<bool> DeleteAsync(Guid conversationParticipantId);
}