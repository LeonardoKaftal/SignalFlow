namespace SignalFlowBackend.Dto;

public record UserDto(
    Guid Id,
    string Email,
    string PasswordHash,
    string Username
);