
namespace SignalFlowBackend.Dto;

public record ChatConversationDto(
    Guid ConversationId,
    string Name,
    bool IsGlobal,
    DateTime CreatedAt
);