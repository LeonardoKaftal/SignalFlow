using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Data;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Repository;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> FindUserEntityByIdAsync(Guid id)
    {
        return await context.Users.FindAsync(id);
    }

    public async Task<User?> FindUserEntityByUsernameAsync(string username)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserDto?> FindUserByIdAsync(Guid id)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(MapUserToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto?> FindUserByUsernameAsync(string username)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Username == username)
            .Select(MapUserToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto?> FindUserByEmailAsync(string email)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Email == email)
            .Select(MapUserToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto?> SaveUserAsync(User user)
    {
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return await FindUserByIdAsync(user.Id);
    }

    public async Task UpdateUserAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task<UserDto?> DeleteUserAsync(User user)
    {
        var dto = await FindUserByIdAsync(user.Id);
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return dto;
    }

    private static readonly Expression<Func<User, UserDto>> MapUserToDto =
        user => new UserDto(
            Id: user.Id,
            Email: user.Email,
            Username: user.Username,
            Token: null,
            RefreshTokenExpiryTime: user.RefreshTokenExpiryTime
        );
}