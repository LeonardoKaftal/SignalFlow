namespace SignalFlowBackend.Dto;

public record CreateConversationRequestDto(
    string Name,
    IEnumerable<Guid> UserIds
);

