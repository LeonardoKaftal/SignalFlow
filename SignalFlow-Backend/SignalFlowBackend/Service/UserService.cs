using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;

namespace SignalFlowBackend.Service;

public class UserService(IPasswordHasher<User> hasher, IUserRepository userRepository): IUserService
{
    public async Task<User?> FindUserByIdAsync(Guid id)
    {
        return await userRepository.FindUserByIdAsync(id);
    }

    public async Task<UserDto?> FindByUsernameAsync(string username)
    {
        var found = await userRepository.FindByUsernameAsync(username);
        if (found is null) return null;
        return MapUserToUserDto(found);
    }

    public async Task<Guid?> RegisterAsync(UserRegisterRequestDto request)
    {
        if (!MailAddress.TryCreate(request.Email, out var _)) return null;
        
        var found = await userRepository.FindByUsernameAsync(request.Username);
        if (found is not null) return null;
        
        var toCreate = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = string.Empty,
            RegistrationTime = DateTime.UtcNow
        };

        var passwordHash = hasher.HashPassword(toCreate, request.Password);
        toCreate.PasswordHash = passwordHash;

        var created = await userRepository.SaveUserAsync(toCreate);
        return created?.Id;
    }

    public async Task<User?> LoginAsync(UserLoginRequest registerRequest)
    {
        var found = await userRepository.FindByUsernameAsync(registerRequest.Username);
        if (found is null) return null;

        var success = hasher
                          .VerifyHashedPassword(found, found.PasswordHash, registerRequest.Password) == 
                           PasswordVerificationResult.Success;

        return success ? found : null;
    }
    
    private UserDto? MapUserToUserDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            Email: user.Email,
            PasswordHash: user.PasswordHash,
            Username: user.Username
        );
    }
}