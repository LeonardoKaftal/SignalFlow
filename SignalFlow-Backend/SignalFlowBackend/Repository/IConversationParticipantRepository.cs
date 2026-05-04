using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public interface IConversationParticipantRepository
{
    Task<ConversationParticipant?> GetParticipantEntityByIdAsync(Guid conversationParticipantId);
    Task<ConversationParticipantDto?> GetParticipantByIdAsync(Guid conversationParticipantId);
    Task<ConversationParticipantDto?> GetParticipantByUserIdAndConversationId(Guid userId, Guid conversationId);
    Task<IEnumerable<ConversationParticipantDto>> GetAllParticipantsByConversationId(Guid conversationId);
    Task<List<ConversationParticipantDto>?> GetAllAdminsByConversationId(Guid conversationId);
    Task<int> GetNumberOfParticipantsByConversationId(Guid conversationId);
    Task<ConversationParticipantDto> SaveAsync(ConversationParticipant participant);
    Task<bool> DeleteAsync(Guid conversationParticipantId);
}