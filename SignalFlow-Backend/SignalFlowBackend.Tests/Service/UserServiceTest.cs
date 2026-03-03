using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Repository;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.Service;

[TestSubject(typeof(UserService))]
public class UserServiceTest
{
    [Fact]
    public async Task RegisterUserAsync_WithValidRequest_ReturnsUserDtoWithIdAndToken()
    {
        // given
        var expectedId = Guid.NewGuid();
        var userRequest = new UserRegisterRequestDto(
            Username: "Jonh",
            Email: "jonh@gmail.com",
            Password: "password"
        );

        var createdUser = new User
        {
            Id = expectedId,
            Username = userRequest.Username,
            Email = userRequest.Email,
            PasswordHash = "hashed-password",
            RegistrationTime = DateTime.UtcNow,
            RefreshToken = "aabbccdd",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        passwordHasher.HashPassword(Arg.Any<User>(), userRequest.Password)
                      .Returns("hashed-password");

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByUsernameAsync(userRequest.Username)
                      .Returns(Task.FromResult<User?>(null));
        userRepository.SaveUserAsync(Arg.Any<User>())
                      .Returns(Task.FromResult<User?>(createdUser));

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateToken(createdUser).Returns("generated-token");
        tokenService.GenerateRefreshToken().Returns(createdUser.RefreshToken);

        var service = new UserService(passwordHasher, userRepository, tokenService);

        // when
        var actual = await service.RegisterAsync(userRequest);

        // then
        actual.Should().NotBeNull();
        actual!.Id.Should().Be(expectedId);
        actual.Username.Should().Be(userRequest.Username);
        actual.Email.Should().Be(userRequest.Email);
        actual.Token.Should().Be("generated-token");
        actual.RefreshToken.Should().Be(createdUser.RefreshToken);
    }

    [Fact]
    public async Task RegisterUserAsync_WithInvalidEmail_ReturnsNull()
    {
        // given
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var tokenService = Substitute.For<ITokenService>();
        var userService = new UserService(passwordHasher, userRepository, tokenService);

        var userRequest = new UserRegisterRequestDto(
            Username: "Jonh",
            Email: "InvalidEmailGmail.com",
            Password: "password"
        );

        // when
        var actual = await userService.RegisterAsync(userRequest);

        // then
        actual.Should().BeNull();
    }

    [Fact]
    public async Task RegisterUserAsync_WithAlreadyUsedUsername_ReturnsNull()
    {
        // given
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var tokenService = Substitute.For<ITokenService>();
        var userService = new UserService(passwordHasher, userRepository, tokenService);

        var userRequest = new UserRegisterRequestDto(
            Username: "Jonh",
            Email: "JonhA@gmail.com",
            Password: "password"
        );

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "Jonh",
            Email = "JonhB@gmail.com",
            PasswordHash = "password",
            RegistrationTime = DateTime.UtcNow,
            RefreshToken = null,
            RefreshTokenExpiryTime = default
        };

        userRepository.FindByUsernameAsync(userRequest.Username)
                      .Returns(Task.FromResult<User?>(existingUser));

        // when
        var actual = await userService.RegisterAsync(userRequest);

        // then
        actual.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithNotExistingUsername_ReturnsNull()
    {
        // given
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var userRepository = Substitute.For<IUserRepository>();
        var tokenService = Substitute.For<ITokenService>();
        var service = new UserService(passwordHasher, userRepository, tokenService);

        var request = new UserLoginRequest(
            Username: "Jonh",
            Password: "password"
        );

        userRepository.FindByUsernameAsync(request.Username)
                      .Returns(Task.FromResult<User?>(null));

        // when
        var result = await service.LoginAsync(request);

        // then
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithValidPassword_ReturnsUserDtoWithToken()
    {
        // given
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var userRepository = Substitute.For<IUserRepository>();
        var tokenService = Substitute.For<ITokenService>();
        var service = new UserService(passwordHasher, userRepository, tokenService);

        var request = new UserLoginRequest("Jonh", "password");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = "jonh@gmail.com",
            PasswordHash = "hashed-password",
            RegistrationTime = DateTime.UtcNow,
            RefreshToken = null,
            RefreshTokenExpiryTime = default
        };

        userRepository.FindByUsernameAsync(request.Username)
                      .Returns(Task.FromResult<User?>(user));

        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                      .Returns(PasswordVerificationResult.Success);

        tokenService.GenerateToken(user).Returns("generated-token");

        // when
        var result = await service.LoginAsync(request);

        // then
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be(user.Username);
        result.Email.Should().Be(user.Email);
        result.Token.Should().Be("generated-token");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // given
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var userRepository = Substitute.For<IUserRepository>();
        var tokenService = Substitute.For<ITokenService>();
        var service = new UserService(passwordHasher, userRepository, tokenService);

        var request = new UserLoginRequest("Jonh", "wrong-password");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = "jonh@gmail.com",
            PasswordHash = "hashed-password",
            RegistrationTime = DateTime.UtcNow,
            RefreshToken = null,
            RefreshTokenExpiryTime = default
        };

        userRepository.FindByUsernameAsync(request.Username)
                      .Returns(Task.FromResult<User?>(user));

        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                      .Returns(PasswordVerificationResult.Failed);

        // when
        var result = await service.LoginAsync(request);

        // then
        result.Should().BeNull();
    }
}
