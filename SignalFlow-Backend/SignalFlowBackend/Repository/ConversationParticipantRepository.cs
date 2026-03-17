using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Data;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

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

    public async Task<IEnumerable<ConversationParticipantDto>> GetAllParticipantByConversationId(Guid conversationId)
    {
        return await context.Participants
            .AsNoTracking()
            .Where(p => p.ConversationId == conversationId)
            .Select(MapParticipantToDto)
            .ToListAsync();
    }

    public async Task<ConversationParticipantDto> SaveAsync(ConversationParticipant participant)
    {
        context.Participants.Update(participant);
        await context.SaveChangesAsync();
        return MapParticipant(participant);
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
            LastAccess: p.LastAccess
        );
    
    // same as MapParticipantToDto but as a method and not as expression 
    private static ConversationParticipantDto MapParticipant(ConversationParticipant p)
    {
       return  new ConversationParticipantDto(
           ConversationParticipantId: p.ConversationParticipantId,
           Username: p.User.Username,
           LastAccess: p.LastAccess
       );
    }
}