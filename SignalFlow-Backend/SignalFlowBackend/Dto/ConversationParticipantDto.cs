namespace SignalFlowBackend.Dto;

public record ConversationParticipantDto(
    Guid ConversationParticipantId,
    string Username,
    DateTime LastAccess  
);