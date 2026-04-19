namespace SignalFlowBackend.Dto;

public record UserRegisterRequestDto(
    string Username,
    string Email,
    string Password);