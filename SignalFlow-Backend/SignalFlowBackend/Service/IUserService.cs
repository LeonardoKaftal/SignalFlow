using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Service;

public interface IUserService
{
    public Task<UserDto?> FindByIdAsync(Guid id);
    public Task<UserDto?> FindByUsernameAsync(string username);
    public Task<UserDto?> RegisterAsync(UserRegisterRequestDto request);
    public Task<UserDto?> LoginAsync(UserLoginRequest registerRequest);
    public Task<UserDto?> LoginWithRefreshTokenAsync(RefreshTokenRequest tokenRequest);
}