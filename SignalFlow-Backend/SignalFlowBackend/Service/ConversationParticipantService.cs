using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;

namespace SignalFlowBackend.Service;

public class ConversationParticipantService(
    IConversationParticipantRepository conversationParticipantRepository,
    IUserRepository userRepository
) : IConversationParticipantService
{
    public Task<ConversationParticipantDto?> GetParticipantByIdAsync(Guid conversationParticipantId)
    {
        return conversationParticipantRepository.GetParticipantByIdAsync(conversationParticipantId);
    }

    public Task<ConversationParticipantDto?> GetParticipantByUserIdAndConversationIdAsync(Guid userId, Guid conversationId)
    {
        return conversationParticipantRepository.GetParticipantByUserIdAndConversationId(userId, conversationId);
    }

    public Task<IEnumerable<ConversationParticipantDto>> GetAllParticipantsByConversationIdAsync(Guid conversationId)
    {
        return conversationParticipantRepository.GetAllParticipantByConversationId(conversationId);
    }

    public async Task<ConversationParticipantDto?> SaveParticipantAsync(Guid userId, Guid conversationId)
    {
        var existingParticipant = await conversationParticipantRepository
            .GetParticipantByUserIdAndConversationId(userId, conversationId);

        if (existingParticipant is not null)
            return existingParticipant;

        var user = await userRepository.FindUserEntityByIdAsync(userId);
        if (user is null)
            return null;

        var participant = new ConversationParticipant
        {
            UserId = userId,
            User = user,
            ConversationId = conversationId,
            ChatConversation = null!,
            LastAccess = DateTime.UtcNow
        };

        return await conversationParticipantRepository.SaveAsync(participant);
    }

    public Task<bool> DeleteParticipantAsync(Guid conversationParticipantId)
    {
        return conversationParticipantRepository.DeleteAsync(conversationParticipantId);
    }
}

