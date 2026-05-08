using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Exceptions;
using SignalFlowBackend.Repository;

namespace SignalFlowBackend.Service;

public class MessageService(
    IMessageRepository messageRepository,
    IConversationParticipantService conversationParticipantService) : IMessageService
{
    public async Task<IEnumerable<MessageDto>?> GetAllMessagesByConversationId(Guid conversationId)
    {
        return await messageRepository.GetAllMessagesByConversationId(conversationId);
    }

    public async Task<IEnumerable<MessageDto>?> GetAllMessagesByConversationIdAndConversationParticipantId(
        Guid conversationId,
        Guid conversationParticipantId)
    {
        return await messageRepository.GetAllMessagesByConversationIdAndUserId(conversationId, conversationParticipantId);
    }

    public async Task<MessageDto?> GetMessageById(Guid messageId)
    {
        return await messageRepository.GetMessageById(messageId);
    }

    public async Task<MessageDto?> SaveMessage(MessageDto toSave)
    {
        if (toSave.ConversationId == Guid.Empty || toSave.SenderId is null || string.IsNullOrWhiteSpace(toSave.Content))
            return null;

        var sender = await conversationParticipantService
            .GetParticipantEntityByIdAsync((Guid)toSave.SenderId);
        if (sender is null || sender.ConversationId != toSave.ConversationId)
            return null;

        var message = new Message
        {
            ConversationId = toSave.ConversationId,
            SenderId = toSave.SenderId,
            SentTime = DateTime.UtcNow,
            Content = toSave.Content.Trim()
        };

        var saved = await messageRepository.Save(message);
        if (saved is null) return null;

        sender.LastMessageRead = saved.MessageId; 

        await conversationParticipantService.UpdateParticipantAsync(sender);
        return saved;
    }

    public async Task<bool> DeleteMessage(Guid messageId)
    {
        return await messageRepository.Delete(messageId);
    }

    public async Task<MessageDto?> UpdateMessage(Guid messageId, string newContent)
    {
        return await messageRepository.Update(messageId, newContent);
    }

    public async Task<MessageDto?> GetLatestMessageByConversationId(Guid conversationId)
    {
        return await messageRepository.GetLatestMessageByConversationId(conversationId);
    }

    public async Task<IEnumerable<MessageDto>?> GetAllMessagesByUserId(Guid userId)
    {
        return await messageRepository.GetAllMessagesByUserId(userId);
    }
}