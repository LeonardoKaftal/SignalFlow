using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;
using SignalFlowBackend.Role;

namespace SignalFlowBackend.Service;

public class ConversationParticipantService(
    IConversationParticipantRepository conversationParticipantRepository,
    IUserRepository userRepository
) : IConversationParticipantService
{
    private async Task<bool> IsRequesterAdminAsync(Guid requesterParticipantId, Guid conversationId)
    {
        var requester = await conversationParticipantRepository.GetParticipantEntityByIdAsync(requesterParticipantId);

        return requester is not null &&
               requester.ConversationId == conversationId &&
               requester.Role == ConversationParticipantRole.Admin;
    }

    public async Task<ConversationParticipant?> GetParticipantEntityByIdAsync(Guid conversationParticipantId)
    {
        return await conversationParticipantRepository.GetParticipantEntityByIdAsync(conversationParticipantId);
    }

    public Task<ConversationParticipantDto?> GetParticipantByIdAsync(Guid conversationParticipantId)
    {
        return conversationParticipantRepository.GetParticipantByIdAsync(conversationParticipantId);
    }

    public Task<ConversationParticipantDto?> GetParticipantByUserIdAndConversationIdAsync(Guid userId, Guid conversationId)
    {
        return conversationParticipantRepository.GetParticipantByUserIdAndConversationId(userId, conversationId);
    }

    public async Task<bool> IsUserParticipantOfConversationAsync(Guid userId, Guid conversationId)
    {
        return await conversationParticipantRepository.GetParticipantByUserIdAndConversationId(userId, conversationId)
            is not null;
    }

    public Task<IEnumerable<ConversationParticipantDto>> GetAllParticipantsByConversationIdAsync(Guid conversationId)
    {
        return conversationParticipantRepository.GetAllParticipantByConversationId(conversationId);
    }

    public async Task<ConversationParticipantDto?> SaveParticipantAsync(Guid userId, Guid conversationId, Guid? requesterParticipantId = null)
    {
        if (requesterParticipantId is not null)
        {
            var requesterIsAdmin = await IsRequesterAdminAsync(requesterParticipantId.Value, conversationId);
            if (!requesterIsAdmin)
                return null;
        }

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

    public async Task<bool> DeleteParticipantAsync(Guid conversationParticipantId, Guid conversationId, Guid? requesterParticipantId = null)
    {
        var participantFound =
            await conversationParticipantRepository.GetParticipantEntityByIdAsync(conversationParticipantId);

        if (participantFound is null || participantFound.ConversationId != conversationId)
            return false;

        if (requesterParticipantId is not null && requesterParticipantId != conversationParticipantId)
        {
            var requesterIsAdmin = await IsRequesterAdminAsync(requesterParticipantId.Value, conversationId);
            if (!requesterIsAdmin)
                return false;
        }

        return await conversationParticipantRepository.DeleteAsync(conversationParticipantId);
    }

    public async Task<bool> AddAdministratorToConversation(Guid adminParticipantId, Guid conversationId, Guid? requesterParticipantId = null)
    {
        var participantFound =
            await conversationParticipantRepository.GetParticipantEntityByIdAsync(adminParticipantId);

        if (participantFound is null || participantFound.ConversationId != conversationId)
            return false;

        if (requesterParticipantId is not null)
        {
            var requesterIsAdmin = await IsRequesterAdminAsync(requesterParticipantId.Value, conversationId);
            if (!requesterIsAdmin)
                return false;
        }

        if (participantFound.Role == ConversationParticipantRole.Admin)
            return true;

        participantFound.Role = ConversationParticipantRole.Admin;
        await conversationParticipantRepository.SaveAsync(participantFound);

        return true;
    }
}

