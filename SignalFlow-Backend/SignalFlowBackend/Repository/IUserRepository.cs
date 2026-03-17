using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public interface IUserRepository
{
    public Task<User?> FindUserEntityByIdAsync(Guid id);
    Task<User?> FindUserEntityByUsernameAsync(string username);
    public Task<UserDto?> FindUserByIdAsync(Guid id);
    public Task<UserDto?> FindUserByUsernameAsync(string username);
    public Task<UserDto?> FindUserByEmailAsync(string email);
    public Task<UserDto?> SaveUserAsync(User user);
    public Task UpdateUserAsync(User found);
}