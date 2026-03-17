using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;

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

    public Task<bool> DeleteConversationAsync(Guid conversationId)
    {
        return conversationRepository.DeleteAsync(conversationId);
    }

    public async Task<ChatConversationDto?> CreateConversationAsync(string name, IEnumerable<Guid> userIds)
    {
        if (string.IsNullOrWhiteSpace(name))
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

        foreach (var userId in userIdList)
        {
            var participant = await conversationParticipantService.SaveParticipantAsync(userId, savedConversation.ConversationId);
            if (participant is null)
            {
                await conversationRepository.DeleteAsync(savedConversation.ConversationId);
                return null;
            }
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

