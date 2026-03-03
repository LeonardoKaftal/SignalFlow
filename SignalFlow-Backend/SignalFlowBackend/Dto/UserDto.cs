namespace SignalFlowBackend.Dto;

public record UserDto(
    Guid Id,
    string Email,
    string Username,
    string? Token,
    string RefreshToken,
    DateTime RefreshTokenExpiryTime
);