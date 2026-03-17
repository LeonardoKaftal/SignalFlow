namespace SignalFlowBackend.Dto;

public record LoginResponseDto(
    Guid Id,
    string Email,
    string Username,
    string? Token,
    DateTime? RefreshTokenExpiryTime,
    string? RefreshToken);