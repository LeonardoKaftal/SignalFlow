namespace SignalFlowBackend.Dto;

public record MessageDto(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderId,
    string Username,
    DateTime SentTime,
    string Content
);