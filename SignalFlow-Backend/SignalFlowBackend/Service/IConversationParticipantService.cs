using SignalFlowBackend.Dto;

namespace SignalFlowBackend.Service;

public interface IConversationParticipantService
{
    Task<ConversationParticipantDto?> GetParticipantByIdAsync(Guid conversationParticipantId);
    Task<ConversationParticipantDto?> GetParticipantByUserIdAndConversationIdAsync(Guid userId, Guid conversationId);
    Task<IEnumerable<ConversationParticipantDto>> GetAllParticipantsByConversationIdAsync(Guid conversationId);
    Task<ConversationParticipantDto?> SaveParticipantAsync(Guid userId, Guid conversationId);
    Task<bool> DeleteParticipantAsync(Guid conversationParticipantId);
}