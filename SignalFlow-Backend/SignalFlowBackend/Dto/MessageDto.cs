namespace SignalFlowBackend.Dto;

public record MessageDto(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderId,
    DateTime SentTime,
    string Content
);