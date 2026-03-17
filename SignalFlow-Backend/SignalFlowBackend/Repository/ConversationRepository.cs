using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Data;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public class ConversationRepository(AppDbContext context) : IConversationRepository
{
    
    // User id is used for  
    
    public async Task<ChatConversation?> GetConversationEntityByIdAsync(Guid conversationId)
    {
        return await context.Conversations.FindAsync(conversationId);
    }

    public async Task<ChatConversationDto?> GetConversationByIdAsync(Guid conversationId)
    {
        return await context.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.ConversationId == conversationId)
            .Select(MapConversationToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<ChatConversationDto?> GetConversationByNameAndParticipantIdAsync(string name, Guid participantId)
    {
        return await context.Conversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.Name == name &&
                conversation.Participants.Any(participant => participant.ConversationParticipantId == participantId))
            .Select(MapConversationToDto)
            .FirstOrDefaultAsync();
    }

    // given a name and a list of userIds, return the conversation if it exists where every user is a participant of this chat conversation, otherwise return null
    public async Task<ChatConversationDto?> GetConversationByNameAndUserIdsAsync(string name, IEnumerable<Guid> userIds)
    {
        var distinctUserIds = userIds.Distinct().ToList();

        return await context.Conversations
            .AsNoTracking()
            .Where(conversation =>
                !conversation.IsGlobal &&
                conversation.Name == name &&
                conversation.Participants.Count == distinctUserIds.Count &&
                conversation.Participants.Count(participant => distinctUserIds.Contains(participant.UserId)) == distinctUserIds.Count)
            .Select(MapConversationToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ChatConversationDto>> GetAllConversationsByUserIdAsync(Guid userId)
    {
        return await context.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.Participants.Any(participant => participant.UserId == userId))
            .OrderByDescending(conversation => conversation.CreatedAt)
            .Select(MapConversationToDto)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatConversationDto>> GetConversationsByNameAndUserIdAsync(string name, Guid userId)
    {
        return await context.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.Name == name && conversation.Participants.Any(participant => participant.UserId == userId))
            .OrderByDescending(conversation => conversation.CreatedAt)
            .Select(MapConversationToDto)
            .ToListAsync();
    }
    
    

    public async Task<ChatConversationDto?> GetGlobalConversationAsync()
    {
        return await context.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.IsGlobal)
            .OrderBy(conversation => conversation.CreatedAt)
            .Select(MapConversationToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<ChatConversationDto?> SaveAsync(ChatConversation conversation)
    {
        var result = await context.Conversations.AddAsync(conversation);
        await context.SaveChangesAsync();
        return MapConversation(result.Entity);
    }

    public async Task<bool> DeleteAsync(Guid conversationId)
    {
        var conversation = await context.Conversations.FindAsync(conversationId);

        if (conversation is null)
            return false;

        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync();
        return true;
    }

    private static readonly Expression<Func<ChatConversation, ChatConversationDto>> MapConversationToDto =
        conversation => new ChatConversationDto(
            ConversationId: conversation.ConversationId,
            Name: conversation.Name,
            IsGlobal: conversation.IsGlobal,
            CreatedAt: conversation.CreatedAt
        );

    private static ChatConversationDto MapConversation(ChatConversation conversation)
    {
        return new ChatConversationDto(
            ConversationId: conversation.ConversationId,
            Name: conversation.Name,
            IsGlobal: conversation.IsGlobal,
            CreatedAt: conversation.CreatedAt
        );
    }
}

