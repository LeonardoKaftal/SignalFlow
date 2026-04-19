using System.Text.Json.Serialization;

namespace SignalFlowBackend.Dto;

public record LoginResponseDto(
    Guid Id,
    string Email,
    string Username,
    string? Token,
    DateTime? RefreshTokenExpiryTime,
    [property: JsonIgnore] string? RefreshToken);
