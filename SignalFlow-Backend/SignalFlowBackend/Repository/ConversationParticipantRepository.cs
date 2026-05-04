using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Data;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Role;

namespace SignalFlowBackend.Repository;

public class ConversationParticipantRepository(AppDbContext context) 
    : IConversationParticipantRepository
{
    public async Task<ConversationParticipant?> GetParticipantEntityByIdAsync(Guid conversationParticipantId)
    {
        return await context
            .Participants
            .FindAsync(conversationParticipantId);
    }

    public async Task<ConversationParticipantDto?> GetParticipantByIdAsync(Guid conversationParticipantId)
    {
        return await context.Participants
            .AsNoTracking()
            .Where(p => p.ConversationParticipantId == conversationParticipantId)
            .Select(MapParticipantToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<ConversationParticipantDto?> GetParticipantByUserIdAndConversationId(Guid userId, Guid conversationId)
    {
        return await context.Participants
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ConversationId == conversationId)
            .Select(MapParticipantToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ConversationParticipantDto>> GetAllParticipantsByConversationId(Guid conversationId)
    {
        return await context.Participants
            .AsNoTracking()
            .Where(p => p.ConversationId == conversationId)
            .Select(MapParticipantToDto)
            .ToListAsync();
    }

    // it can return null only if conversationId is the one of the global conversation, as there are no admin there
    public async Task<List<ConversationParticipantDto>?> GetAllAdminsByConversationId(Guid conversationId)
    {
        return await context
            .Participants
            .AsNoTracking()
            .Where(participant => participant.Role == ConversationParticipantRole.Admin && participant.ConversationId == conversationId)
            .Select(MapParticipantToDto)
            .ToListAsync();
    }

    public async Task<int> GetNumberOfParticipantsByConversationId(Guid conversationId)
    {
        return await context
                .Participants
                .CountAsync(p => p.ConversationId == conversationId);
    }

    public async Task<ConversationParticipantDto> SaveAsync(ConversationParticipant participant)
    {
        context.Participants.Update(participant);
        await context.SaveChangesAsync();
        return await context.Participants
            .AsNoTracking()
            .Where(p => p.ConversationParticipantId == participant.ConversationParticipantId)
            .Select(MapParticipantToDto)
            .FirstAsync();
    }

    public async Task<bool> DeleteAsync(Guid conversationParticipantId)
    {
        var participant = await context.Participants.FindAsync(conversationParticipantId);

        if (participant == null)
            return false;

        context.Participants.Remove(participant);
        await context.SaveChangesAsync();
        return true;
    }

    private static readonly Expression<Func<ConversationParticipant, ConversationParticipantDto>> MapParticipantToDto =
        p => new ConversationParticipantDto(
            ConversationParticipantId: p.ConversationParticipantId,
            Username: p.User.Username,
            Role: p.Role,
            LastAccess: p.LastAccess
        );
}