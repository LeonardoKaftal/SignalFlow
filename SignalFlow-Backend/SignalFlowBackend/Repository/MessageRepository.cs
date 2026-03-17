using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Data;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public class MessageRepository(AppDbContext context) : IMessageRepository
{
    public async Task<IEnumerable<MessageDto>> GetAllMessagesByConversationId(Guid conversationId)
    {
        return await context
            .Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentTime)
            .Select(MapMessageToDto)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageDto>> GetAllMessagesByConversationIdAndUserId(
        Guid conversationId,
        Guid conversationParticipantId)
    {
        return await context
            .Messages
            .AsNoTracking()
            .Where(m =>
                m.ConversationId == conversationId &&
                m.SenderId == conversationParticipantId)
            .OrderBy(m => m.SentTime)
            .Select(MapMessageToDto)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageDto>> GetAllMessagesByUserId(Guid userId)
    {
        return await context
            .Messages
            .AsNoTracking()
            .Where(m => m.Sender.UserId == userId)
            .OrderByDescending(m => m.SentTime)
            .Select(MapMessageToDto)
            .ToListAsync();
    }

    public async Task<Message?> GetMessageEntityById(Guid messageId)
    {
        return await context.Messages.FindAsync(messageId);
    }

    public async Task<MessageDto?> GetMessageById(Guid messageId)
    {
        return await context
            .Messages
            .AsNoTracking()
            .Where(m => m.MessageId == messageId)
            .Select(MapMessageToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<MessageDto?> Save(Message message)
    {
        var result = await context.Messages.AddAsync(message);
        await context.SaveChangesAsync();
        return MapMessage(result.Entity);
    }

    public async Task<bool> Delete(Guid messageId)
    {
        var message = await context.Messages.FindAsync(messageId);

        if (message is null)
            return false;

        context.Messages.Remove(message);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<MessageDto?> Update(Guid messageId, string newContent)
    {
        var message = await context.Messages.FindAsync(messageId);
        if (message is null) return null;

        message.Content = newContent;
        await context.SaveChangesAsync();
        return MapMessage(message);
    }

    public async Task<MessageDto?> GetLatestMessageByConversationId(Guid conversationId)
    {
        return await context
            .Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentTime)
            .Select(MapMessageToDto)
            .FirstOrDefaultAsync();
    }

    private static readonly Expression<Func<Message, MessageDto>> MapMessageToDto =
        message => new MessageDto(
            MessageId: message.MessageId,
            ConversationId: message.ConversationId,
            SenderId: message.SenderId,
            SentTime: message.SentTime,
            Content: message.Content);
   
    // same as MapMessageToDto but as a method and not as expression
    private static MessageDto MapMessage(Message message)
    {
        return new MessageDto(
            message.MessageId,
            message.ConversationId,
            message.SenderId,
            message.SentTime,
            message.Content);
    }
    
}