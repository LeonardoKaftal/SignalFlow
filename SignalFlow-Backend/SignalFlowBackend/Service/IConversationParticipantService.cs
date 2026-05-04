using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Service;

public interface IConversationParticipantService
{
    Task<ConversationParticipant?> GetParticipantEntityByIdAsync(Guid conversationParticipantId); 
    Task<ConversationParticipantDto?> GetParticipantByIdAsync(Guid conversationParticipantId);
    Task<ConversationParticipantDto?> GetParticipantByUserIdAndConversationIdAsync(Guid userId, Guid conversationId);
    Task<bool> IsUserParticipantOfConversationAsync(Guid userId, Guid conversationId);
    Task<IEnumerable<ConversationParticipantDto>> GetAllParticipantsByConversationIdAsync(Guid conversationId);
    Task<ConversationParticipantDto?> SaveParticipantAsync(Guid userId, Guid conversationId, Guid? requesterParticipantId = null);
    Task<bool?> DeleteParticipantAsync(Guid conversationParticipantId, Guid conversationId, Guid requesterParticipantId);
    Task<bool> AddAdministratorToConversation(Guid adminParticipantId, Guid conversationId, Guid? requesterParticipantId = null);
}