using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Data;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public class UserRepository(AppDbContext context): IUserRepository
{
    public async Task<User?> FindByIdAsync(Guid id)
    {
        return await context.Users.FindAsync(id);
    }

    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await context
            .Users
            .FirstOrDefaultAsync(user => user.Username.Equals(username));
    }

    public async Task<User?> SaveUserAsync(User user)
    {
        var found = await context.AddAsync(user);
        await context.SaveChangesAsync();
        return found.Entity;
    }

    public async Task UpdateUserAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }
}