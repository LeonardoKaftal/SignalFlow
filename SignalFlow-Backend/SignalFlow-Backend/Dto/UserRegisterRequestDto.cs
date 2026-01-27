namespace SignalFlow_Backend.Dto;

public record UserRequestDto(
    string Username,
    string Email,
    string PasswordHash);