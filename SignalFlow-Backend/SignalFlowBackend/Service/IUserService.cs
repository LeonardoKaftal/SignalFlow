using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Service;

public interface IUserService
{
    public Task<User?> FindEntityByIdAsync(Guid id);
    public Task<UserDto?> FindByIdAsync(Guid id);
    public Task<UserDto?> FindByUsernameAsync(string username);
    public Task<UserDto?> RegisterAsync(UserRegisterRequestDto request);
    public Task<LoginResponseDto?> LoginAsync(UserLoginRequest registerRequest);
    public Task<LoginResponseDto?> LoginWithRefreshTokenAsync(RefreshTokenRequest tokenRequest);
}