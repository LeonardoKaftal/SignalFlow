using SignalFlowBackend.Role;

namespace SignalFlowBackend.Dto;

public record ConversationParticipantDto(
    Guid ConversationParticipantId,
    string Username,
    ConversationParticipantRole Role,
    DateTime LastAccess  
);