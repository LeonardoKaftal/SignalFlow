using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public interface IUserRepository
{
    
    public Task<User?> FindByIdAsync(Guid id);
    public Task<User?> FindByUsernameAsync(string username);
    public Task<User?> SaveUserAsync(User user);
    public Task UpdateUserAsync(User found);
}