using System.Net.Mail;
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

        return MapUserToUserDto(user: found, token: null, refreshToken: null);
    }

    public async Task<UserDto?> FindByUsernameAsync(string username)
    {
        var found = await userRepository.FindByUsernameAsync(username);
        if (found is null) return null;
        return MapUserToUserDto(user: found, token: null, refreshToken: null);
    }
    

    public async Task<UserDto?> RegisterAsync(UserRegisterRequestDto request)
    {
        if (!MailAddress.TryCreate(request.Email, out var _)) return null;
        var existingEmail = await userRepository.FindByEmailAsync(request.Email);
        if (existingEmail is not null) return null; 
        
        var found = await userRepository.FindByUsernameAsync(request.Username);
        if (found is not null) return null;

        
        var toCreate = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = string.Empty,
            RegistrationTime = DateTime.UtcNow,
            RefreshTokenHash = string.Empty,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        var passwordHash = hasher.HashPassword(toCreate, request.Password);
        toCreate.PasswordHash = passwordHash;

        var refreshToken = tokenService.GenerateRefreshToken();
        toCreate.RefreshTokenHash = hasher.HashPassword(toCreate, refreshToken);
        
        var created = await userRepository.SaveUserAsync(toCreate);
        if (created is null) return null;

        var token = tokenService.GenerateToken(created);
        return MapUserToUserDto(created, token, refreshToken);
    }

    public async Task<UserDto?> LoginAsync(UserLoginRequest registerRequest)
    {
        var found = await userRepository.FindByUsernameAsync(registerRequest.Username);
        if (found is null) return null;

        var result = hasher.VerifyHashedPassword(found, found.PasswordHash, registerRequest.Password);

        switch (result)
        {
            case PasswordVerificationResult.Failed:
                return null;
            case PasswordVerificationResult.SuccessRehashNeeded:
                found.PasswordHash = hasher.HashPassword(found, registerRequest.Password);
                await userRepository.UpdateUserAsync(found);
                break;
        }

        var token = tokenService.GenerateToken(found);
        var refreshToken = tokenService.GenerateRefreshToken();
        found.RefreshTokenHash = hasher.HashPassword(found, refreshToken);
        found.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        
        await userRepository.UpdateUserAsync(found);
        return MapUserToUserDto(found, token, refreshToken);
    }

    public async Task<UserDto?> LoginWithRefreshTokenAsync(RefreshTokenRequest refreshTokenRequest)
    {
        var found = await userRepository.FindByIdAsync(refreshTokenRequest.Id);
        if (found is null) return null;

        if (found.RefreshTokenExpiryTime <= DateTime.UtcNow) return null;

        var refreshTokenVerification = hasher
            .VerifyHashedPassword(found,
                found.RefreshTokenHash,
                refreshTokenRequest.Token);
        if (refreshTokenVerification == PasswordVerificationResult.Failed) return null;

        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newToken = tokenService.GenerateToken(found);
        
        found.RefreshTokenHash = hasher.HashPassword(found, newRefreshToken);
        found.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        
        await userRepository.UpdateUserAsync(found);
        return MapUserToUserDto(found, newToken, newRefreshToken);
    }

    private UserDto? MapUserToUserDto(User user, string? token, string? refreshToken)
    {
        return new UserDto(
            Id: user.Id,
            Email: user.Email,
            Username: user.Username,
            Token: token,
            RefreshToken: refreshToken,
            RefreshTokenExpiryTime: user.RefreshTokenExpiryTime
        );
    }
}