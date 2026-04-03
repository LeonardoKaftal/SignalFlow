using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalFlowBackend.Data;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Role;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.IntegrationTests;

public class ConversationParticipantServiceIntegrationTest(IntegrationTestWebAppFactory factory)
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

    private static ChatConversation BuildConversation()
    {
        return new ChatConversation
        {
            ConversationId = Guid.NewGuid(),
            Name = $"conv-{ShortSuffix()}",
            IsGlobal = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetParticipantByIdAsync_ShouldReturnParticipantDto_WhenParticipantExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var user = BuildUser();
        var conversation = BuildConversation();
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
        var result = await service.GetParticipantByIdAsync(participant.ConversationParticipantId);

        // then
        result.Should().NotBeNull();
        result!.ConversationParticipantId.Should().Be(participant.ConversationParticipantId);
        result.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task GetParticipantByUserIdAndConversationIdAsync_ShouldReturnParticipantDto_WhenParticipantExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var user = BuildUser();
        var conversation = BuildConversation();

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            LastAccess = DateTime.UtcNow
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.GetParticipantByUserIdAndConversationIdAsync(user.Id, conversation.ConversationId);

        // then
        result.Should().NotBeNull();
        result!.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task GetAllParticipantsByConversationIdAsync_ShouldReturnAllParticipants_WhenConversationHasParticipants()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var conversation = BuildConversation();
        var userOne = BuildUser();
        var userTwo = BuildUser();

        await dbContext.Users.AddRangeAsync([userOne, userTwo], cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddRangeAsync([
            new ConversationParticipant
            {
                ConversationParticipantId = Guid.NewGuid(),
                UserId = userOne.Id,
                User = userOne,
                ConversationId = conversation.ConversationId,
                ChatConversation = conversation,
                LastAccess = DateTime.UtcNow
            },
            new ConversationParticipant
            {
                ConversationParticipantId = Guid.NewGuid(),
                UserId = userTwo.Id,
                User = userTwo,
                ConversationId = conversation.ConversationId,
                ChatConversation = conversation,
                LastAccess = DateTime.UtcNow
            }
        ], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = (await service.GetAllParticipantsByConversationIdAsync(conversation.ConversationId)).ToList();

        // then
        result.Should().HaveCount(2);
        result.Select(p => p.Username).Should().BeEquivalentTo([userOne.Username, userTwo.Username]);
    }

    [Fact]
    public async Task SaveParticipantAsync_ShouldCreateParticipant_WhenParticipantDoesNotExist()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var user = BuildUser();
        var conversation = BuildConversation();

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.SaveParticipantAsync(user.Id, conversation.ConversationId);

        // then
        result.Should().NotBeNull();
        result!.Username.Should().Be(user.Username);

        var count = await dbContext.Participants.CountAsync(cancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task SaveParticipantAsync_ShouldReturnExistingParticipant_WhenParticipantAlreadyExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var user = BuildUser();
        var conversation = BuildConversation();
        var existingParticipantId = Guid.NewGuid();

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(new ConversationParticipant
        {
            ConversationParticipantId = existingParticipantId,
            UserId = user.Id,
            User = user,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            LastAccess = DateTime.UtcNow
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.SaveParticipantAsync(user.Id, conversation.ConversationId);

        // then
        result.Should().NotBeNull();
        result!.ConversationParticipantId.Should().Be(existingParticipantId);

        var count = await dbContext.Participants.CountAsync(cancellationToken);
        count.Should().Be(1);
    }

    [Fact]
    public async Task SaveParticipantAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var conversation = BuildConversation();
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.SaveParticipantAsync(Guid.NewGuid(), conversation.ConversationId);

        // then
        result.Should().BeNull();

        var count = await dbContext.Participants.CountAsync(cancellationToken);
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnTrue_WhenParticipantExists()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var user = BuildUser();
        var conversation = BuildConversation();
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
        var deleted = await service.DeleteParticipantAsync(participant.ConversationParticipantId, conversation.ConversationId);

        // then
        deleted.Should().BeTrue();

        var found = await dbContext.Participants.FindAsync([participant.ConversationParticipantId], cancellationToken);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnFalse_WhenParticipantDoesNotExist()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var service = Services.GetRequiredService<IConversationParticipantService>();

        // when
        var deleted = await service.DeleteParticipantAsync(Guid.NewGuid(), Guid.NewGuid());

        // then
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task SaveParticipantAsync_ShouldReturnNull_WhenRequesterIsNotAdmin()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var requesterUser = BuildUser();
        var targetUser = BuildUser();
        var conversation = BuildConversation();
        var requesterParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = requesterUser.Id,
            User = requesterUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync([requesterUser, targetUser], cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(requesterParticipant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var created = await service.SaveParticipantAsync(
            targetUser.Id,
            conversation.ConversationId,
            requesterParticipant.ConversationParticipantId);

        // then
        created.Should().BeNull();

        var participants = await dbContext.Participants
            .AsNoTracking()
            .Where(p => p.ConversationId == conversation.ConversationId)
            .ToListAsync(cancellationToken);

        participants.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnFalse_WhenRequesterIsNotAdmin()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var requesterUser = BuildUser();
        var targetUser = BuildUser();
        var conversation = BuildConversation();
        var requesterParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = requesterUser.Id,
            User = requesterUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        };
        var targetParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = targetUser.Id,
            User = targetUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync([requesterUser, targetUser], cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddRangeAsync([requesterParticipant, targetParticipant], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var deleted = await service.DeleteParticipantAsync(
            targetParticipant.ConversationParticipantId,
            conversation.ConversationId,
            requesterParticipant.ConversationParticipantId);

        // then
        deleted.Should().BeFalse();

        var found = await dbContext.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ConversationParticipantId == targetParticipant.ConversationParticipantId, cancellationToken);

        found.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAdministratorToConversation_ShouldReturnFalse_WhenRequesterIsNotAdmin()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var requesterUser = BuildUser();
        var targetUser = BuildUser();
        var conversation = BuildConversation();
        var requesterParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = requesterUser.Id,
            User = requesterUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        };
        var targetParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = targetUser.Id,
            User = targetUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync([requesterUser, targetUser], cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddRangeAsync([requesterParticipant, targetParticipant], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var promoted = await service.AddAdministratorToConversation(
            targetParticipant.ConversationParticipantId,
            conversation.ConversationId,
            requesterParticipant.ConversationParticipantId);

        // then
        promoted.Should().BeFalse();

        var targetRole = await dbContext.Participants
            .AsNoTracking()
            .Where(p => p.ConversationParticipantId == targetParticipant.ConversationParticipantId)
            .Select(p => p.Role)
            .FirstAsync(cancellationToken);

        targetRole.Should().Be(ConversationParticipantRole.Regular);
    }

    [Fact]
    public async Task AddAdministratorToConversation_ShouldReturnTrue_WhenRequesterIsAdmin()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var requesterUser = BuildUser();
        var targetUser = BuildUser();
        var conversation = BuildConversation();
        var requesterParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = requesterUser.Id,
            User = requesterUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Admin,
            LastAccess = DateTime.UtcNow
        };
        var targetParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = targetUser.Id,
            User = targetUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastAccess = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync([requesterUser, targetUser], cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddRangeAsync([requesterParticipant, targetParticipant], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var promoted = await service.AddAdministratorToConversation(
            targetParticipant.ConversationParticipantId,
            conversation.ConversationId,
            requesterParticipant.ConversationParticipantId);

        // then
        promoted.Should().BeTrue();

        var targetRole = await dbContext.Participants
            .AsNoTracking()
            .Where(p => p.ConversationParticipantId == targetParticipant.ConversationParticipantId)
            .Select(p => p.Role)
            .FirstAsync(cancellationToken);

        targetRole.Should().Be(ConversationParticipantRole.Admin);
    }
}

