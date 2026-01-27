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
public class UserServiceTest()
{
    
    [Fact]
    public async Task RegisterUserAsync_WithValidRequest_ReturnId()
    {
        // given
        var expected = Guid.NewGuid();

        var userRequest = new UserRegisterRequestDto(
            Username: "Jonh",
            Email: "jonh@gmail.com",
            Password: "password"
        );

        var createdUser = new User
        {
            Id = expected,
            Username = userRequest.Username,
            Email = userRequest.Email,
            PasswordHash = "hashed-password",
            RegistrationTime = DateTime.UtcNow
        };

        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var userRepository = Substitute.For<IUserRepository>();

        passwordHasher
            .HashPassword(Arg.Any<User>(), userRequest.Password)
            .Returns("hashed-password");

        userRepository
            .FindByUsernameAsync(userRequest.Username)
            .Returns(Task.FromResult<User?>(null));

        userRepository
            .SaveUserAsync(Arg.Any<User>())
            .Returns(Task.FromResult<User?>(createdUser));

        var service = new UserService(passwordHasher, userRepository);

        // when
        var actual = await service.RegisterAsync(userRequest);

        // then
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task RegisterUserAsync_WithInvalidEmail_ReturnNull()
    {
        // given
        var repository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For <IPasswordHasher<User>>();
        var userService = new UserService(passwordHasher, repository);
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
    public async Task RegisterUserAsync_WithAlreadyUsedUsername_ReturnNull()
    {
        // given
        var repository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For <IPasswordHasher<User>>();
        var userService = new UserService(passwordHasher, repository);
        var userRequest = new UserRegisterRequestDto(
            Username: "Jonh",
            Email: "JonhA@gmail.com",
            Password: "password"
        );
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "JonhB@gmail.com",
            Username = "Jonh",
            PasswordHash = "password",
            RegistrationTime = DateTime.UtcNow,
        };
        
        // when
        repository
            .FindByUsernameAsync(userRequest.Username)
            .Returns(Task.FromResult<User?>(existingUser));
        var actual = await userService.RegisterAsync(userRequest);
        
        // then
        actual.Should().BeNull();
    } 
    
    [Fact]
    public async Task LoginAsync_WithNotExistingUsername_ReturnNull()
    {
        // given
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var repository = Substitute.For<IUserRepository>();
        var service = new UserService(passwordHasher, repository);

        var request = new UserLoginRequest(
            Username: "Jonh",
            Password: "password"
        );

        repository
            .FindByUsernameAsync(request.Username)
            .Returns(Task.FromResult<User?>(null));

        // when
        var result = await service.LoginAsync(request);

        // then
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithValidPassword_ReturnUser()
    {
        // given
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var repository = Substitute.For<IUserRepository>();
        var service = new UserService(passwordHasher, repository);

        var request = new UserLoginRequest(
            Username: "Jonh",
            Password: "password"
        );

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = "jonh@gmail.com",
            PasswordHash = "hashed-password",
            RegistrationTime = DateTime.UtcNow
        };

        repository
            .FindByUsernameAsync(request.Username)
            .Returns(Task.FromResult<User?>(user));

        passwordHasher
            .VerifyHashedPassword(user, user.PasswordHash, request.Password)
            .Returns(PasswordVerificationResult.Success);

        // when
        var result = await service.LoginAsync(request);

        // then
        result.Should().NotBeNull();
        result.Should().Be(user);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnNull()
    {
        // given
        var passwordHasher = Substitute.For<IPasswordHasher<User>>();
        var repository = Substitute.For<IUserRepository>();
        var service = new UserService(passwordHasher, repository);

        var request = new UserLoginRequest(
            Username: "Jonh",
            Password: "wrong-password"
        );

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = "jonh@gmail.com",
            PasswordHash = "hashed-password",
            RegistrationTime = DateTime.UtcNow
        };

        repository
            .FindByUsernameAsync(request.Username)
            .Returns(Task.FromResult<User?>(user));

        passwordHasher
            .VerifyHashedPassword(user, user.PasswordHash, request.Password)
            .Returns(PasswordVerificationResult.Failed);

        // when
        var result = await service.LoginAsync(request);

        // then
        result.Should().BeNull();
    }

}