using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public interface IConversationRepository
{
    Task<ChatConversation?> GetConversationEntityByIdAsync(Guid conversationId);
    Task<ChatConversationDto?> GetConversationByIdAsync(Guid conversationId);

    Task<ChatConversationDto?> GetConversationByNameAndParticipantIdAsync(string name, Guid participantId);
    // given a name and a list of userIds, return the conversation if it exists where every user is a participant of this chat conversation, otherwise return null
    Task<ChatConversationDto?> GetConversationByNameAndUserIdsAsync(string name, IEnumerable<Guid> userIds);
    Task<IEnumerable<ChatConversationDto>> GetAllConversationsByUserIdAsync(Guid userId);
    Task<IEnumerable<ChatConversationDto>> GetConversationsByNameAndUserIdAsync(string name, Guid userId);
    Task<ChatConversationDto?> GetGlobalConversationAsync();
    Task<ChatConversationDto?> SaveAsync(ChatConversation conversation);
    Task<bool> DeleteAsync(Guid conversationId);
}

