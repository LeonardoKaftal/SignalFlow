using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalFlowBackend.Data;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.IntegrationTests;

public class UserServiceIntegrationTest(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static UserRegisterRequestDto BuildValidRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new UserRegisterRequestDto(
            Username: $"user-{suffix}",
            Email: $"m-{suffix}@sf.it",
            Password: "Password123!");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnUserDtoAndPersistUser_WhenRequestIsValid()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var userService = Services.GetRequiredService<IUserService>();
        var request = BuildValidRequest();

        // when
        var result = await userService.RegisterAsync(request);

        // then
        result.Should().NotBeNull();
        Assert.NotNull(result);
        result.Username.Should().Be(request.Username);
        result.Email.Should().Be(request.Email);
        result.Token.Should().NotBeNullOrWhiteSpace();

        var savedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == result.Id, cancellationToken);
        savedUser.Should().NotBeNull();

        var globalConversation = await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsGlobal, cancellationToken);
        globalConversation.Should().NotBeNull();
        Assert.NotNull(globalConversation);

        var participant = await dbContext.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == result.Id && p.ConversationId == globalConversation.ConversationId,
                cancellationToken);
        participant.Should().NotBeNull();
    }

    [Fact]
    public async Task FindByIdAsync_ShouldReturnUserDto_WhenUserExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var userService = Services.GetRequiredService<IUserService>();
        var request = BuildValidRequest();
        var created = await userService.RegisterAsync(request);
        Assert.NotNull(created);

        // when
        var found = await userService.FindByIdAsync(created.Id);

        // then
        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);
        found.Username.Should().Be(request.Username);
        found.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task FindByUsernameAsync_ShouldReturnUserDto_WhenUserExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var userService = Services.GetRequiredService<IUserService>();
        var request = BuildValidRequest();
        await userService.RegisterAsync(request);

        // when
        var found = await userService.FindByUsernameAsync(request.Username);

        // then
        found.Should().NotBeNull();
        found!.Username.Should().Be(request.Username);
        found.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnLoginResponseDto_WhenCredentialsAreValid()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var userService = Services.GetRequiredService<IUserService>();
        var request = BuildValidRequest();
        await userService.RegisterAsync(request);

        var loginRequest = new UserLoginRequest(request.Username, request.Password);

        // when
        var result = await userService.LoginAsync(loginRequest);

        // then
        result.Should().NotBeNull();
        result!.Username.Should().Be(request.Username);
        result.Email.Should().Be(request.Email);
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var userService = Services.GetRequiredService<IUserService>();
        var request = BuildValidRequest();
        await userService.RegisterAsync(request);

        var loginRequest = new UserLoginRequest(request.Username, "WrongPassword!");

        // when
        var result = await userService.LoginAsync(loginRequest);

        // then
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginWithRefreshTokenAsync_ShouldReturnLoginResponseDto_WhenRefreshTokenIsValid()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var userService = Services.GetRequiredService<IUserService>();
        var registerRequest = BuildValidRequest();
        await userService.RegisterAsync(registerRequest);

        var loginResult = await userService.LoginAsync(new UserLoginRequest(registerRequest.Username, registerRequest.Password));
        Assert.NotNull(loginResult);

        var refreshRequest = new RefreshTokenRequest(loginResult.Id, loginResult.RefreshToken!);

        // when
        var refreshed = await userService.LoginWithRefreshTokenAsync(refreshRequest);

        // then
        refreshed.Should().NotBeNull();
        refreshed!.Id.Should().Be(loginResult.Id);
        refreshed.Token.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBe(loginResult.RefreshToken);
    }

    [Fact]
    public async Task LoginWithRefreshTokenAsync_ShouldReturnNull_WhenRefreshTokenIsExpired()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var userService = Services.GetRequiredService<IUserService>();
        var registerRequest = BuildValidRequest();
        var created = await userService.RegisterAsync(registerRequest);
        Assert.NotNull(created);

        var loginResult = await userService.LoginAsync(new UserLoginRequest(registerRequest.Username, registerRequest.Password));
        Assert.NotNull(loginResult);

        var userEntity = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == loginResult.Id, cancellationToken);
        Assert.NotNull(userEntity);
        userEntity.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1);
        dbContext.Users.Update(userEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var refreshRequest = new RefreshTokenRequest(loginResult.Id, loginResult.RefreshToken!);

        // when
        var refreshed = await userService.LoginWithRefreshTokenAsync(refreshRequest);

        // then
        refreshed.Should().BeNull();
    }
}



