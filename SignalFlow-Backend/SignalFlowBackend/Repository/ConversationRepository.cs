using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Data;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public class ConversationRepository(AppDbContext context) : IConversationRepository
{
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
            .Where(conversation =>
                conversation.Name == name &&
                conversation.Participants.Any(participant => participant.UserId == userId))
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
        await context.Conversations.AddAsync(conversation);
        await context.SaveChangesAsync();
        return await GetConversationByIdAsync(conversation.ConversationId);
    }

    public async Task<bool> DeleteAsync(Guid conversationId)
    {
        var exists = await context.Conversations
            .AsNoTracking()
            .AnyAsync(conversation => conversation.ConversationId == conversationId);

        if (!exists)
            return false;

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await context.Messages
                .Where(message => message.ConversationId == conversationId)
                .ExecuteDeleteAsync();

            await context.Participants
                .Where(participant => participant.ConversationId == conversationId)
                .ExecuteDeleteAsync();

            var deletedConversations = await context.Conversations
                .Where(conversation => conversation.ConversationId == conversationId)
                .ExecuteDeleteAsync();

            if (deletedConversations == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static readonly Expression<Func<ChatConversation, ChatConversationDto>> MapConversationToDto =
        conversation => new ChatConversationDto(
            ConversationId: conversation.ConversationId,
            Name: conversation.Name,
            IsGlobal: conversation.IsGlobal,
            CreatedAt: conversation.CreatedAt
        );
}