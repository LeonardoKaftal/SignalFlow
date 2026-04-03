using SignalFlowBackend.Dto;

namespace SignalFlowBackend.Service;

public interface IConversationService
{
    Task<ChatConversationDto?> GetConversationByIdAsync(Guid conversationId);
    Task<IEnumerable<ChatConversationDto>> GetAllConversationsByUserIdAsync(Guid userId);
    Task<ChatConversationDto?> GetConversationByNameAndParticipantIdAsync(string name, Guid participantId);
    Task<IEnumerable<ChatConversationDto>> GetConversationsByNameAndUserIdAsync(string name, Guid userId);
    Task<ChatConversationDto?> CreateConversationAsync(string name, IEnumerable<Guid>? userIds);
    Task<ChatConversationDto?> GetOrCreateGlobalConversationAsync();
    Task<bool> DeleteConversationAsync(Guid conversationId, Guid requesterParticipantId);
}

