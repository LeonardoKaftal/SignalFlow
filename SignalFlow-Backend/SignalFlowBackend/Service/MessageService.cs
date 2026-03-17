using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;

namespace SignalFlowBackend.Service;

public class MessageService(IMessageRepository messageRepository) : IMessageService
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
        var message = new Message
        {
            ConversationId = toSave.ConversationId,
            SenderId = toSave.SenderId,
            SentTime = DateTime.UtcNow,
            Content = toSave.Content
        };

        return await messageRepository.Save(message);
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