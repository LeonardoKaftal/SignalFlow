using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Exceptions;
using SignalFlowBackend.Repository;

namespace SignalFlowBackend.Service;

public class UserService(
    IPasswordHasher<User> hasher,
    IUserRepository userRepository,
    ITokenService tokenService,
    IConversationService conversationService,
    IConversationParticipantService conversationParticipantService
    
) : IUserService
{

    public async Task<User?> FindEntityByIdAsync(Guid id)
    {
        return await userRepository.FindUserEntityByIdAsync(id);
    }
    
    public Task<UserDto?> FindByIdAsync(Guid id)
    {
        return userRepository.FindUserByIdAsync(id);
    }

    
    public Task<UserDto?> FindByUsernameAsync(string username)
    {
        return userRepository.FindUserByUsernameAsync(username);
    }

    
    public async Task<UserDto?> RegisterAsync(UserRegisterRequestDto request)
    {
        if (!MailAddress.TryCreate(request.Email, out _))
            return null;

        var existingEmail = await userRepository.FindUserByEmailAsync(request.Email);
        if (existingEmail is not null)
            return null;

        var existingUsername = await userRepository.FindUserByUsernameAsync(request.Username);
        if (existingUsername is not null)
            return null;

        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = "",
            RegistrationTime = DateTime.UtcNow,
            RefreshTokenHash = "",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        user.PasswordHash = hasher.HashPassword(user, request.Password);

        var refreshToken = tokenService.GenerateRefreshToken();
        user.RefreshTokenHash = hasher.HashPassword(user, refreshToken);

        var created = await userRepository.SaveUserAsync(user);
        if (created is null)
            return null;

        var token = tokenService.GenerateToken(user);
        try
        {
            // Register the user in the global chat
            var globalConversation = await conversationService.GetOrCreateGlobalConversationAsync();

            if (globalConversation is null)
                throw new ChatNotFoundException("INTERNAL SERVER ERROR: Global chat has not been found nor created, aborting registration");

            var participant = await conversationParticipantService
                .SaveParticipantAsync(user.Id, globalConversation.ConversationId);

            if (participant is null)
                throw new ChatNotFoundException("INTERNAL SERVER ERROR: Cannot register the user in global chat, aborting registration");
        }
        catch
        {
            await userRepository.DeleteUserAsync(user);
            throw;
        }
        
        // return the token
        return created with
        {
            Token = token
        };
    }

    
    public async Task<LoginResponseDto?> LoginAsync(UserLoginRequest request)
    {
        var userEntity = await userRepository
            .FindUserEntityByUsernameAsync(request.Username);

        if (userEntity is null)
            return null;

        var verification = hasher.VerifyHashedPassword(
            userEntity,
            userEntity.PasswordHash,
            request.Password
        );

        switch (verification)
        {
            case PasswordVerificationResult.Failed:
                return null;
            case PasswordVerificationResult.SuccessRehashNeeded:
                userEntity.PasswordHash = hasher.HashPassword(userEntity, request.Password);
                break;
        }

        var token = tokenService.GenerateToken(userEntity);

        var refreshToken = tokenService.GenerateRefreshToken();

        userEntity.RefreshTokenHash = hasher.HashPassword(userEntity, refreshToken);
        userEntity.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await userRepository.UpdateUserAsync(userEntity);

        return new LoginResponseDto(
            Id: userEntity.Id,
            Username: userEntity.Username,
            Email: userEntity.Email,
            Token: token,
            RefreshTokenExpiryTime: userEntity.RefreshTokenExpiryTime,
            RefreshToken: refreshToken
        );
    }

    
    public async Task<LoginResponseDto?> LoginWithRefreshTokenAsync(
        RefreshTokenRequest request)
    {
        var user = await userRepository.FindUserEntityByIdAsync(request.Id);

        if (user is null)
            return null;

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return null;

        var verification = hasher.VerifyHashedPassword(
            user,
            user.RefreshTokenHash,
            request.Token
        );

        if (verification == PasswordVerificationResult.Failed)
            return null;

        var newToken = tokenService.GenerateToken(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        user.RefreshTokenHash = hasher.HashPassword(user, newRefreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await userRepository.UpdateUserAsync(user);

        return new LoginResponseDto(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            Token: newToken,
            RefreshTokenExpiryTime: user.RefreshTokenExpiryTime,
            RefreshToken: newRefreshToken
        );
    }
}