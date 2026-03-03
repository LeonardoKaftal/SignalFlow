using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;

namespace SignalFlowBackend.Service;

public class UserService(IPasswordHasher<User> hasher, IUserRepository userRepository, ITokenService tokenService): IUserService
{
    public async Task<UserDto?> FindByIdAsync(Guid id)
    {
        var found = await userRepository.FindByIdAsync(id);
        if (found is null) return null;

        return MapUserToUserDto(user: found, token: null);
    }

    public async Task<UserDto?> FindByUsernameAsync(string username)
    {
        var found = await userRepository.FindByUsernameAsync(username);
        if (found is null) return null;
        return MapUserToUserDto(user: found, token: null);
    }
    

    public async Task<UserDto?> RegisterAsync(UserRegisterRequestDto request)
    {
        if (!MailAddress.TryCreate(request.Email, out var _)) return null;
        
        var found = await userRepository.FindByUsernameAsync(request.Username);
        if (found is not null) return null;

        
        var toCreate = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = string.Empty,
            RegistrationTime = DateTime.UtcNow,
            RefreshToken = tokenService.GenerateRefreshToken(),
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        var passwordHash = hasher.HashPassword(toCreate, request.Password);
        toCreate.PasswordHash = passwordHash;

        var created = await userRepository.SaveUserAsync(toCreate);
        if (created is null) return null;

        var token = tokenService.GenerateToken(created);
        return MapUserToUserDto(created, token);
    }

    public async Task<UserDto?> LoginAsync(UserLoginRequest registerRequest)
    {
        var found = await userRepository.FindByUsernameAsync(registerRequest.Username);
        if (found is null) return null;

        var success = hasher
                          .VerifyHashedPassword(found, found.PasswordHash, registerRequest.Password) == 
                           PasswordVerificationResult.Success;

        if (!success) return null;

        var token = tokenService.GenerateToken(found);
        found.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        
        await userRepository.UpdateUserAsync(found);
        return MapUserToUserDto(found, token);
    }

    public async Task<UserDto?> LoginWithRefreshTokenAsync(RefreshTokenRequest refreshTokenRequest)
    {
        var found = await userRepository.FindByIdAsync(refreshTokenRequest.Id);
        if (found is null) return null;

        if (found.RefreshTokenExpiryTime <= DateTime.UtcNow) return null;

        var refreshTokenIsValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(found.RefreshToken),
            Encoding.UTF8.GetBytes(refreshTokenRequest.Token)
        );

        if (!refreshTokenIsValid) return null;

        found.RefreshToken = tokenService.GenerateRefreshToken();
        found.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        
        await userRepository.UpdateUserAsync(found);
        return MapUserToUserDto(found, found.RefreshToken);
    }

    private UserDto? MapUserToUserDto(User user, string? token)
    {
        return new UserDto(
            Id: user.Id,
            Email: user.Email,
            Username: user.Username,
            Token: token,
            RefreshToken: user.RefreshToken,
            RefreshTokenExpiryTime: user.RefreshTokenExpiryTime
        );
    }
}