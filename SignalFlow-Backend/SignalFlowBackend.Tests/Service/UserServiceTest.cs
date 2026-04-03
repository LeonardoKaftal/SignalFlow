using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Exceptions;
using SignalFlowBackend.Repository;
using SignalFlowBackend.Role;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.Service;

[TestSubject(typeof(UserService))]
public class UserServiceTest
{
    [Fact]
    public async Task RegisterAsync_ShouldReturnUserDto_WhenValid()
    {
        // Given 
        var request = new UserRegisterRequestDto("John", "john@gmail.com", "password");
        var expectedId = Guid.NewGuid();

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.HashPassword(Arg.Any<User>(), request.Password).Returns("hashed-password");

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.FindUserByUsernameAsync(request.Username).Returns((UserDto?)null);
        userRepository.FindUserByEmailAsync(request.Email).Returns((UserDto?)null);

        var expectedExpiryTime = DateTime.UtcNow.AddDays(7);
        var createdUserDto = new UserDto(
            expectedId,
            request.Email,
            request.Username,
            null,
            expectedExpiryTime
        );
        userRepository.SaveUserAsync(Arg.Any<User>()).Returns(createdUserDto);

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateToken(Arg.Any<User>()).Returns("generated-token");
        tokenService.GenerateRefreshToken().Returns("refresh-token");

        var conversationService = Substitute.For<IConversationService>();
        conversationService.GetOrCreateGlobalConversationAsync().Returns(new ChatConversationDto(
            Guid.NewGuid(),
            "Global",
            true,
            DateTime.UtcNow
        ));

        var participantService = Substitute.For<IConversationParticipantService>();
        participantService
            .SaveParticipantAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new ConversationParticipantDto(Guid.NewGuid(), "John", ConversationParticipantRole.Regular, DateTime.UtcNow));

        var service = new UserService(passwordHasher, userRepository, tokenService, conversationService, participantService);

        // When
        var result = await service.RegisterAsync(request);

        // Then
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedId);
        result.Username.Should().Be(request.Username);
        result.Email.Should().Be(request.Email);
        result.Token.Should().Be("generated-token");
        result.RefreshTokenExpiryTime.Should().Be(expectedExpiryTime);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailInvalid()
    {
        // Given
        var conversationService = Substitute.For<IConversationService>();
        var participantService = Substitute.For<IConversationParticipantService>();

        var service = new UserService(Substitute.For<IPasswordHasher<User>>(),
                                      Substitute.For<IUserRepository>(),
                                      Substitute.For<ITokenService>(),
                                      conversationService,
                                      participantService);

        var request = new UserRegisterRequestDto("John", "invalid-email", "password");

        var result = await service.RegisterAsync(request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnLoginResponseDto_WhenValid()
    {
        // Given
        var request = new UserLoginRequest("John", "password");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = "john@gmail.com",
            PasswordHash = "hashed-password",
            RegistrationTime = DateTime.UtcNow,
            RefreshTokenHash = "hashed-refresh",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                      .Returns(PasswordVerificationResult.Success);
        passwordHasher.HashPassword(user, Arg.Any<string>()).Returns("rehashed");

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.FindUserEntityByUsernameAsync(request.Username).Returns(user);

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateToken(user).Returns("generated-token");
        tokenService.GenerateRefreshToken().Returns("refresh-token");

        var conversationService = Substitute.For<IConversationService>();
        var participantService = Substitute.For<IConversationParticipantService>();

        var service = new UserService(passwordHasher, userRepository, tokenService, conversationService, participantService);

        // When
        var result = await service.LoginAsync(request);

        // Then
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be(user.Username);
        result.Email.Should().Be(user.Email);
        result.Token.Should().Be("generated-token");
        result.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenPasswordInvalid()
    {
        var request = new UserLoginRequest("John", "wrong-password");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = "john@gmail.com",
            PasswordHash = "hashed-password",
            RegistrationTime = DateTime.UtcNow,
            RefreshTokenHash = "hashed-refresh",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                      .Returns(PasswordVerificationResult.Failed);

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.FindUserEntityByUsernameAsync(request.Username).Returns(user);

        var tokenService = Substitute.For<ITokenService>();

        var conversationService = Substitute.For<IConversationService>();
        var participantService = Substitute.For<IConversationParticipantService>();

        var service = new UserService(passwordHasher, userRepository, tokenService, conversationService, participantService);

        var result = await service.LoginAsync(request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_ShouldRollbackAndThrow_WhenGlobalParticipantCreationFails()
    {
        // Given
        var request = new UserRegisterRequestDto("John", "john@gmail.com", "password");
        var expectedId = Guid.NewGuid();

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.HashPassword(Arg.Any<User>(), request.Password).Returns("hashed-password");
        passwordHasher.HashPassword(Arg.Any<User>(), Arg.Any<string>()).Returns("hashed-refresh");

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.FindUserByUsernameAsync(request.Username).Returns((UserDto?)null);
        userRepository.FindUserByEmailAsync(request.Email).Returns((UserDto?)null);
        userRepository.SaveUserAsync(Arg.Any<User>()).Returns(new UserDto(
            expectedId,
            request.Email,
            request.Username,
            null,
            DateTime.UtcNow.AddDays(7)
        ));

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateToken(Arg.Any<User>()).Returns("generated-token");
        tokenService.GenerateRefreshToken().Returns("refresh-token");

        var conversationService = Substitute.For<IConversationService>();
        conversationService.GetOrCreateGlobalConversationAsync().Returns(new ChatConversationDto(
            Guid.NewGuid(),
            "Global",
            true,
            DateTime.UtcNow
        ));

        var participantService = Substitute.For<IConversationParticipantService>();
        participantService
            .SaveParticipantAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns((ConversationParticipantDto?)null);

        var service = new UserService(passwordHasher, userRepository, tokenService, conversationService, participantService);

        // When
        Func<Task> act = async () => await service.RegisterAsync(request);

        // Then
        await act.Should().ThrowAsync<ChatNotFoundException>();
        await userRepository.Received(1).DeleteUserAsync(Arg.Any<User>());
    }
}