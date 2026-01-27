using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Service;

public interface IUserService
{
    public Task<User?> FindUserByIdAsync(Guid id);
    public Task<UserDto?> FindByUsernameAsync(string username);
    public Task<Guid?> RegisterAsync(UserRegisterRequestDto request);
    public Task<User?> LoginAsync(UserLoginRequest registerRequest);
}