using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Exceptions;
using SignalFlowBackend.Repository;
using SignalFlowBackend.Role;

namespace SignalFlowBackend.Service;

public class ConversationService(
    IConversationRepository conversationRepository,
    IConversationParticipantService conversationParticipantService
) : IConversationService
{
    private const string GlobalConversationName = "Global";

    public Task<ChatConversationDto?> GetConversationByIdAsync(Guid conversationId)
    {
        return conversationRepository.GetConversationByIdAsync(conversationId);
    }

    public async Task<ChatConversationDto?> GetConversationByNameAndParticipantIdAsync(string name, Guid participantId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await conversationRepository.GetConversationByNameAndParticipantIdAsync(name.Trim(), participantId);
    }
    
    
    public Task<IEnumerable<ChatConversationDto>> GetAllConversationsByUserIdAsync(Guid userId)
    {
        return conversationRepository.GetAllConversationsByUserIdAsync(userId);
    }

    public Task<IEnumerable<ChatConversationDto>> GetConversationsByNameAndUserIdAsync(string name, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(Enumerable.Empty<ChatConversationDto>());

        return conversationRepository.GetConversationsByNameAndUserIdAsync(name.Trim(), userId);
    }
    
    public async Task<bool> DeleteConversationAsync(Guid conversationId, Guid requesterParticipantId)
    {
        var participants = (await conversationParticipantService
            .GetAllParticipantsByConversationIdAsync(conversationId)).ToList();

        var found = participants
            .FirstOrDefault(participant => participant.ConversationParticipantId == requesterParticipantId);
        
        // only an admin may delete a conversation
        if (found is null || found.Role != ConversationParticipantRole.Admin)
            return false;

        return await conversationRepository.DeleteAsync(conversationId);
    }  

    // userIds is the list of the chat participant to create and add to the conversation
    public async Task<ChatConversationDto?> CreateConversationAsync(string name, IEnumerable<Guid>? userIds)
    {
        if (string.IsNullOrWhiteSpace(name) || userIds is null)
            return null;

        var normalizedName = name.Trim();
        var userIdList = userIds
            .Distinct()
            .ToList();

        if (userIdList.Count < 2)
            return null;

        var existingConversation = await conversationRepository
            .GetConversationByNameAndUserIdsAsync(normalizedName, userIdList);

        if (existingConversation is not null)
            return null;

        var conversation = new ChatConversation
        {
            Name = normalizedName,
            IsGlobal = false,
            CreatedAt = DateTime.UtcNow
        };

        var savedConversation = await conversationRepository.SaveAsync(conversation);
        if (savedConversation is null)
            return null;

        var adminUserId = userIdList.First();
        var adminParticipantId = Guid.Empty;

        foreach (var userId in userIdList)
        {
            var participant = await conversationParticipantService
                .SaveParticipantAsync(userId, savedConversation.ConversationId);

            if (participant is null)
            {
                await conversationRepository.DeleteAsync(savedConversation.ConversationId);
                throw new NotCreatedException("INTERNAL SERVER ERROR: couldn't create the conversation because a participant could not be saved");
            }

            if (userId == adminUserId)
                adminParticipantId = participant.ConversationParticipantId;
        }

        if (adminParticipantId == Guid.Empty)
        {
            await conversationRepository.DeleteAsync(savedConversation.ConversationId);
            throw new NotCreatedException("INTERNAL SERVER ERROR: admin participant not found during conversation creation");
        }

        var success = await conversationParticipantService
            .AddAdministratorToConversation(adminParticipantId, savedConversation.ConversationId);

        if (!success)
        {
            await conversationRepository.DeleteAsync(savedConversation.ConversationId);
            throw new NotCreatedException("INTERNAL SERVER ERROR: couldn't create the conversation because making the creator admin failed");
        }

        return savedConversation;
    }

    public async Task<ChatConversationDto?> GetOrCreateGlobalConversationAsync()
    {
        var existingGlobalConversation = await conversationRepository.GetGlobalConversationAsync();
        if (existingGlobalConversation is not null)
            return existingGlobalConversation;

        var conversation = new ChatConversation
        {
            Name = GlobalConversationName,
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        };

        return await conversationRepository.SaveAsync(conversation);
    }

    
}

