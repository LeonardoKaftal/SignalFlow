namespace SignalFlow_Backend.Dto;

public record UserRegisterResponseDto(
    Guid Id,
    string Username,
    string Email);