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
            LastMessageRead = null
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.GetParticipantByIdAsync(participant.ConversationParticipantId);

        // then
        result.Should().NotBeNull();
        result.ConversationParticipantId.Should().Be(participant.ConversationParticipantId);
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
            LastMessageRead = null
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.GetParticipantByUserIdAndConversationIdAsync(user.Id, conversation.ConversationId);

        // then
        result.Should().NotBeNull();
        result.Username.Should().Be(user.Username);
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
                LastMessageRead = null
            },
            new ConversationParticipant
            {
                ConversationParticipantId = Guid.NewGuid(),
                UserId = userTwo.Id,
                User = userTwo,
                ConversationId = conversation.ConversationId,
                ChatConversation = conversation,
                LastMessageRead = null
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
        result.Username.Should().Be(user.Username);
        result.LastMessageRead.Should().BeNull();

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
            LastMessageRead = null
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var result = await service.SaveParticipantAsync(user.Id, conversation.ConversationId);

        // then
        result.Should().NotBeNull();
        result.ConversationParticipantId.Should().Be(existingParticipantId);

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
            LastMessageRead = null
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(participant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var deleted = await service.DeleteParticipantAsync(participant.ConversationParticipantId, conversation.ConversationId, participant.ConversationParticipantId);

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
        var deleted = await service.DeleteParticipantAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

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
            LastMessageRead = null
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
            LastMessageRead = null
        };
        var targetParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = targetUser.Id,
            User = targetUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastMessageRead = null
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
            LastMessageRead = null
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
            LastMessageRead = null
        };
        var targetParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = targetUser.Id,
            User = targetUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastMessageRead = null
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

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnFalse_WhenLastAdminAttemptsToLeaveWithOtherParticipants()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var adminUser = BuildUser();
        var regularUser = BuildUser();
        var conversation = BuildConversation();
        
        var adminParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = adminUser.Id,
            User = adminUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Admin,
            LastMessageRead = null
        };
        
        var regularParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = regularUser.Id,
            User = regularUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastMessageRead = null
        };

        await dbContext.Users.AddRangeAsync([adminUser, regularUser], cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddRangeAsync([adminParticipant, regularParticipant], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var deleted = await service.DeleteParticipantAsync(
            adminParticipant.ConversationParticipantId,
            conversation.ConversationId,
            adminParticipant.ConversationParticipantId);

        // then
        deleted.Should().BeNull();

        var found = await dbContext.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ConversationParticipantId == adminParticipant.ConversationParticipantId, cancellationToken);

        found.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnTrue_WhenLastAdminLeavesAndIsTheOnlyParticipant()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var adminUser = BuildUser();
        var conversation = BuildConversation();
        
        var adminParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = adminUser.Id,
            User = adminUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Admin,
            LastMessageRead = null
        };

        await dbContext.Users.AddAsync(adminUser, cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddAsync(adminParticipant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // when
        var deleted = await service.DeleteParticipantAsync(
            adminParticipant.ConversationParticipantId,
            conversation.ConversationId,
            adminParticipant.ConversationParticipantId);

        // then
        deleted.Should().BeTrue();

        var found = await dbContext.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ConversationParticipantId == adminParticipant.ConversationParticipantId, cancellationToken);

        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteParticipantAsync_ShouldReturnFalse_WhenOtherAdminTriesToRemoveLastAdminWithOtherParticipants()
    {
        // given
        var cancellationToken = TestContext.Current.CancellationToken;
        await Factory.ResetDatabaseAsync(cancellationToken);

        var dbContext = Services.GetRequiredService<AppDbContext>();
        var service = Services.GetRequiredService<IConversationParticipantService>();

        var targetAdminUser = BuildUser();
        var requesterAdminUser = BuildUser();
        var regularUser = BuildUser();
        var conversation = BuildConversation();
        
        var targetAdminParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = targetAdminUser.Id,
            User = targetAdminUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Admin,
            LastMessageRead = null
        };

        var requesterAdminParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = requesterAdminUser.Id,
            User = requesterAdminUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Admin,
            LastMessageRead = null
        };
        
        var regularParticipant = new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            UserId = regularUser.Id,
            User = regularUser,
            ConversationId = conversation.ConversationId,
            ChatConversation = conversation,
            Role = ConversationParticipantRole.Regular,
            LastMessageRead = null
        };

        await dbContext.Users.AddRangeAsync([targetAdminUser, requesterAdminUser, regularUser], cancellationToken);
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
        await dbContext.Participants.AddRangeAsync([targetAdminParticipant, requesterAdminParticipant, regularParticipant], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Now remove the requesterAdminParticipant to make targetAdminParticipant the last admin
        await dbContext.Participants.Where(p => p.ConversationParticipantId == requesterAdminParticipant.ConversationParticipantId).ExecuteDeleteAsync(cancellationToken);

        // when - trying to remove the last admin while regular user still exists
        var deleted = await service.DeleteParticipantAsync(
            targetAdminParticipant.ConversationParticipantId,
            conversation.ConversationId,
            targetAdminParticipant.ConversationParticipantId);

        // then
        deleted.Should().BeNull();

        var found = await dbContext.Participants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ConversationParticipantId == targetAdminParticipant.ConversationParticipantId, cancellationToken);

        found.Should().NotBeNull();
    }
}
