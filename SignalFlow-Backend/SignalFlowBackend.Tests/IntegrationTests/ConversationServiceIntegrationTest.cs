using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalFlowBackend.Data;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Exceptions;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.IntegrationTests;

public class ConversationServiceIntegrationTest(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private static string ShortSuffix() => Guid.NewGuid().ToString("N")[..8];

    private static User BuildUser()
    {
        var suffix = ShortSuffix();

        return new User
        {
            Id = Guid.NewGuid(),
            Username = $"u-{suffix}",
            Email = $"m-{suffix}@sf.it",
            PasswordHash = "hash",
            RegistrationTime = DateTime.UtcNow,
            RefreshTokenHash = "refresh-hash",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };
    }

    private static ChatConversation BuildConversation(string? name = null, bool isGlobal = false)
    {
        return new ChatConversation
        {
            ConversationId = Guid.NewGuid(),
            Name = name ?? $"conv-{ShortSuffix()}",
            IsGlobal = isGlobal,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetConversationByIdAsync_ShouldReturnConversation_WhenConversationExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();
        var conversation = BuildConversation();

        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.GetConversationByIdAsync(conversation.ConversationId);

        // then
        result.Should().NotBeNull();
        result!.ConversationId.Should().Be(conversation.ConversationId);
        result.Name.Should().Be(conversation.Name);
    }

    [Fact]
    public async Task GetAllConversationsByUserIdAsync_ShouldReturnConversations_WhenUserIsParticipant()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();

        var user = BuildUser();
        var conversationOne = BuildConversation("Team");
        var conversationTwo = BuildConversation("Project");

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddRangeAsync([conversationOne, conversationTwo], cancellationToken);
        await dbContext.Participants.AddRangeAsync([
            new ConversationParticipant
            {
                ConversationParticipantId = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                ConversationId = conversationOne.ConversationId,
                ChatConversation = conversationOne,
                LastAccess = DateTime.UtcNow
            },
            new ConversationParticipant
            {
                ConversationParticipantId = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                ConversationId = conversationTwo.ConversationId,
                ChatConversation = conversationTwo,
                LastAccess = DateTime.UtcNow
            }
        ], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = (await service.GetAllConversationsByUserIdAsync(user.Id)).ToList();

        // then
        result.Should().HaveCount(2);
        result.Select(c => c.ConversationId)
            .Should()
            .BeEquivalentTo([conversationOne.ConversationId, conversationTwo.ConversationId]);
    }

    [Fact]
    public async Task GetConversationByNameAndParticipantIdAsync_ShouldReturnConversation_WhenParticipantBelongsToConversation()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();

        var user = BuildUser();
        var conversation = BuildConversation("Sprint");
        var participant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            LastAccess = DateTime.UtcNow
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.GetConversationByNameAndParticipantIdAsync(conversation.Name, participant.ConversationParticipantId);

        // then
        result.Should().NotBeNull();
        result!.ConversationId.Should().Be(conversation.ConversationId);
    }

    [Fact]
    public async Task GetConversationsByNameAndUserIdAsync_ShouldReturnEmpty_WhenNameIsBlank()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var service = Services.GetRequiredService<IConversationService>();

        // when
        var result = (await service.GetConversationsByNameAndUserIdAsync("   ", Guid.NewGuid())).ToList();

        // then
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldCreateConversationAndParticipants_WhenInputIsValid()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();

        var userOne = BuildUser();
        var userTwo = BuildUser();
        await dbContext.Users.AddRangeAsync([userOne, userTwo], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.CreateConversationAsync("New project", [userOne.Id, userTwo.Id]);

        // then
        result.Should().NotBeNull();
        result!.IsGlobal.Should().BeFalse();
        result.Name.Should().Be("New project");

        var participants = await dbContext.Participants
            .AsNoTracking()
            .Where(p => p.ConversationId == result.ConversationId)
            .ToListAsync(cancellationToken);

        participants.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldReturnNull_WhenLessThanTwoDistinctUsersAreProvided()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();

        var user = BuildUser();
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.CreateConversationAsync("Only one", [user.Id, user.Id]);

        // then
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldRollbackConversation_WhenAnyUserDoesNotExist()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();

        var existingUser = BuildUser();
        await dbContext.Users.AddAsync(existingUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var missingUserId = Guid.NewGuid();

        // when
        Func<Task> act = async () => await service.CreateConversationAsync("Rollback me", [existingUser.Id, missingUserId]);

        // then
        await act.Should().ThrowAsync<NotCreatedException>();

        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == "Rollback me", cancellationToken);

        conversation.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateGlobalConversationAsync_ShouldCreateConversation_WhenGlobalDoesNotExist()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var service = Services.GetRequiredService<IConversationService>();

        // when
        var result = await service.GetOrCreateGlobalConversationAsync();

        // then
        result.Should().NotBeNull();
        result!.IsGlobal.Should().BeTrue();
        result.Name.Should().Be("Global");
    }

    [Fact]
    public async Task GetOrCreateGlobalConversationAsync_ShouldReturnSameConversation_WhenGlobalAlreadyExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var service = Services.GetRequiredService<IConversationService>();

        // when
        var first = await service.GetOrCreateGlobalConversationAsync();
        var second = await service.GetOrCreateGlobalConversationAsync();

        // then
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.ConversationId.Should().Be(first!.ConversationId);
    }

    [Fact]
    public async Task DeleteConversationAsync_ShouldReturnFalse_WhenConversationDoesNotExist()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();

        var userOne = BuildUser();
        var userTwo = BuildUser();
        await dbContext.Users.AddRangeAsync([userOne, userTwo], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createdConversation = await service.CreateConversationAsync("Delete test", [userOne.Id, userTwo.Id]);
        createdConversation.Should().NotBeNull();

        var adminParticipantId = await dbContext.Participants
            .AsNoTracking()
            .Where(p => p.ConversationId == createdConversation!.ConversationId && p.UserId == userOne.Id)
            .Select(p => p.ConversationParticipantId)
            .FirstAsync(cancellationToken);

        // when
        var deleted = await service.DeleteConversationAsync(Guid.NewGuid(), adminParticipantId);

        // then
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteConversationAsync_ShouldReturnTrue_WhenRequesterIsAdmin()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();

        var userOne = BuildUser();
        var userTwo = BuildUser();
        await dbContext.Users.AddRangeAsync([userOne, userTwo], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createdConversation = await service.CreateConversationAsync("Delete allowed", [userOne.Id, userTwo.Id]);
        createdConversation.Should().NotBeNull();

        var adminParticipantId = await dbContext.Participants
            .AsNoTracking()
            .Where(p => p.ConversationId == createdConversation!.ConversationId && p.UserId == userOne.Id)
            .Select(p => p.ConversationParticipantId)
            .FirstAsync(cancellationToken);

        // when
        var deleted = await service.DeleteConversationAsync(createdConversation.ConversationId, adminParticipantId);

        // then
        deleted.Should().BeTrue();

        var found = await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConversationId == createdConversation.ConversationId, cancellationToken);

        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteConversationAsync_ShouldReturnFalse_WhenRequesterIsNotAdmin()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationService>();

        var userOne = BuildUser();
        var userTwo = BuildUser();
        await dbContext.Users.AddRangeAsync([userOne, userTwo], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createdConversation = await service.CreateConversationAsync("Delete denied", [userOne.Id, userTwo.Id]);
        createdConversation.Should().NotBeNull();

        var regularParticipantId = await dbContext.Participants
            .AsNoTracking()
            .Where(p => p.ConversationId == createdConversation!.ConversationId && p.UserId == userTwo.Id)
            .Select(p => p.ConversationParticipantId)
            .FirstAsync(cancellationToken);

        // when
        var deleted = await service.DeleteConversationAsync(createdConversation.ConversationId, regularParticipantId);

        // then
        deleted.Should().BeFalse();

        var found = await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConversationId == createdConversation.ConversationId, cancellationToken);

        found.Should().NotBeNull();
    }
}