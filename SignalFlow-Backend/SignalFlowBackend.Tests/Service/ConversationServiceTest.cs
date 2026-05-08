using FluentAssertions;
using JetBrains.Annotations;
using NSubstitute;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Entity;
using SignalFlowBackend.Exceptions;
using SignalFlowBackend.Repository;
using SignalFlowBackend.Role;
using SignalFlowBackend.Service;
using Xunit;

namespace SignalFlow.Backend.Service;

[TestSubject(typeof(ConversationService))]
public class ConversationServiceTest
{
    private const string ConversationName = "Project Alpha";

    private static ConversationService BuildSut(
        IConversationRepository? repository = null,
        IConversationParticipantService? participantService = null) =>
        new(
            repository ?? Substitute.For<IConversationRepository>(),
            participantService ?? Substitute.For<IConversationParticipantService>()
        );

    private static readonly ConversationParticipantDto SampleParticipant =
        new(Guid.NewGuid(), "alice", ConversationParticipantRole.Regular, null);

    // ── GetConversationByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetConversationByIdAsync_ShouldReturnConversation_WhenConversationExists()
    {
        // given
        var conversationId = Guid.NewGuid();
        var expected = new ChatConversationDto(conversationId, ConversationName, false, DateTime.UtcNow);
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        repository.GetConversationByIdAsync(conversationId).Returns(expected);

        // when
        var actual = await sut.GetConversationByIdAsync(conversationId);

        // then
        actual.Should().Be(expected);
    }

    // ── GetAllConversationsByUserIdAsync ──────────────────────────────────────

    [Fact]
    public async Task GetAllConversationsByUserIdAsync_ShouldReturnConversations_WhenUserHasConversations()
    {
        // given
        var userId = Guid.NewGuid();
        var expected = new List<ChatConversationDto>
        {
            new(Guid.NewGuid(), "Alpha", false, DateTime.UtcNow),
            new(Guid.NewGuid(), "Beta", false, DateTime.UtcNow)
        };
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        repository.GetAllConversationsByUserIdAsync(userId).Returns(expected);

        // when
        var actual = (await sut.GetAllConversationsByUserIdAsync(userId)).ToList();

        // then
        actual.Should().BeEquivalentTo(expected);
    }

    // ── GetConversationsByNameAndUserIdAsync ──────────────────────────────────

    [Fact]
    public async Task GetConversationsByNameAndUserIdAsync_ShouldReturnConversations_WhenNameMatchesForUser()
    {
        // given
        var userId = Guid.NewGuid();
        var expected = new List<ChatConversationDto>
        {
            new(Guid.NewGuid(), ConversationName, false, DateTime.UtcNow),
            new(Guid.NewGuid(), ConversationName, false, DateTime.UtcNow)
        };
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        repository.GetConversationsByNameAndUserIdAsync(ConversationName, userId).Returns(expected);

        // when
        var actual = (await sut.GetConversationsByNameAndUserIdAsync(ConversationName, userId)).ToList();

        // then
        actual.Should().BeEquivalentTo(expected);
    }

    // ── GetConversationByNameAndParticipantIdAsync ────────────────────────────

    [Fact]
    public async Task GetConversationByNameAndParticipantIdAsync_ShouldReturnConversation_WhenParticipantMatches()
    {
        // given
        var participantId = Guid.NewGuid();
        var expected = new ChatConversationDto(Guid.NewGuid(), ConversationName, false, DateTime.UtcNow);
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        repository.GetConversationByNameAndParticipantIdAsync(ConversationName, participantId).Returns(expected);

        // when
        var actual = await sut.GetConversationByNameAndParticipantIdAsync(ConversationName, participantId);

        // then
        actual.Should().Be(expected);
    }

    // ── CreateConversationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateConversationAsync_ShouldReturnNull_WhenLessThanTwoUserIdsProvided()
    {
        // given
        var userIds = new[] { Guid.NewGuid() };
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        // when
        var actual = await sut.CreateConversationAsync(ConversationName, userIds);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().SaveAsync(Arg.Any<ChatConversation>());
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldReturnNull_WhenEmptyListProvided()
    {
        // given
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        // when
        var actual = await sut.CreateConversationAsync(ConversationName, []);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().SaveAsync(Arg.Any<ChatConversation>());
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldReturnNull_WhenNameIsBlank()
    {
        // given
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        // when
        var actual = await sut.CreateConversationAsync("   ", userIds);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().SaveAsync(Arg.Any<ChatConversation>());
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldReturnNull_WhenConversationWithSameNameAndMembersExists()
    {
        // given
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var existingConversation = new ChatConversationDto(Guid.NewGuid(), ConversationName, false, DateTime.UtcNow);
        var repository = Substitute.For<IConversationRepository>();
        var participantService = Substitute.For<IConversationParticipantService>();
        var sut = BuildSut(repository, participantService);

        repository.GetConversationByNameAndUserIdsAsync(ConversationName, Arg.Any<IEnumerable<Guid>>())
            .Returns(existingConversation);

        // when
        var actual = await sut.CreateConversationAsync(ConversationName, userIds);

        // then
        actual.Should().BeNull();
        await repository.DidNotReceive().SaveAsync(Arg.Any<ChatConversation>());
        await participantService.DidNotReceive().SaveParticipantAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldCreateConversationWithParticipants_WhenAllUsersAreValid()
    {
        // given
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userIds = new[] { userId1, userId2 };
        var expected = new ChatConversationDto(Guid.NewGuid(), ConversationName, false, DateTime.UtcNow);

        var repository = Substitute.For<IConversationRepository>();
        var participantService = Substitute.For<IConversationParticipantService>();
        var sut = BuildSut(repository, participantService);

        var before = DateTime.UtcNow;
        repository.SaveAsync(Arg.Any<ChatConversation>()).Returns(expected);
        participantService.SaveParticipantAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(SampleParticipant);
        participantService.AddAdministratorToConversation(SampleParticipant.ConversationParticipantId, expected.ConversationId).Returns(true);

        // when
        var actual = await sut.CreateConversationAsync(ConversationName, userIds);
        var after = DateTime.UtcNow;

        // then
        actual.Should().Be(expected);
        await repository.Received(1).SaveAsync(Arg.Is<ChatConversation>(c =>
            c.Name == ConversationName &&
            !c.IsGlobal &&
            c.CreatedAt >= before &&
            c.CreatedAt <= after));
        await participantService.Received(1).SaveParticipantAsync(userId1, expected.ConversationId);
        await participantService.Received(1).SaveParticipantAsync(userId2, expected.ConversationId);
        await participantService.Received(1).AddAdministratorToConversation(SampleParticipant.ConversationParticipantId, expected.ConversationId);
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldDeduplicateUserIds_WhenDuplicatesProvided()
    {
        // given
        var userId = Guid.NewGuid();
        var userIds = new[] { userId, userId, Guid.NewGuid() };
        var expected = new ChatConversationDto(Guid.NewGuid(), ConversationName, false, DateTime.UtcNow);

        var repository = Substitute.For<IConversationRepository>();
        var participantService = Substitute.For<IConversationParticipantService>();
        var sut = BuildSut(repository, participantService);

        repository.SaveAsync(Arg.Any<ChatConversation>()).Returns(expected);
        participantService.SaveParticipantAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(SampleParticipant);
        participantService.AddAdministratorToConversation(SampleParticipant.ConversationParticipantId, expected.ConversationId).Returns(true);

        // when
        await sut.CreateConversationAsync(ConversationName, userIds);

        // then
        await participantService.Received(2).SaveParticipantAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateConversationAsync_ShouldRollbackAndThrowNotCreatedException_WhenUserDoesNotExist()
    {
        // given
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userIds = new[] { userId1, userId2 };
        var savedConversation = new ChatConversationDto(Guid.NewGuid(), ConversationName, false, DateTime.UtcNow);

        var repository = Substitute.For<IConversationRepository>();
        var participantService = Substitute.For<IConversationParticipantService>();
        var sut = BuildSut(repository, participantService);

        repository.SaveAsync(Arg.Any<ChatConversation>()).Returns(savedConversation);
        participantService.SaveParticipantAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns((ConversationParticipantDto?)null);

        // when
        Func<Task> act = async () => await sut.CreateConversationAsync(ConversationName, userIds);

        // then
        await act.Should().ThrowAsync<NotCreatedException>();
        await repository.Received(1).DeleteAsync(savedConversation.ConversationId);
    }

    // ── GetOrCreateGlobalConversationAsync ────────────────────────────────────

    [Fact]
    public async Task GetOrCreateGlobalConversationAsync_ShouldReturnExistingConversation_WhenGlobalConversationExists()
    {
        // given
        var existingGlobalConversation = new ChatConversationDto(Guid.NewGuid(), "Global", true, DateTime.UtcNow);
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        repository.GetGlobalConversationAsync().Returns(existingGlobalConversation);

        // when
        var actual = await sut.GetOrCreateGlobalConversationAsync();

        // then
        actual.Should().Be(existingGlobalConversation);
        await repository.DidNotReceive().SaveAsync(Arg.Any<ChatConversation>());
    }

    [Fact]
    public async Task GetOrCreateGlobalConversationAsync_ShouldCreateConversation_WhenGlobalConversationDoesNotExist()
    {
        // given
        var createdGlobalConversation = new ChatConversationDto(Guid.NewGuid(), "Global", true, DateTime.UtcNow);
        var repository = Substitute.For<IConversationRepository>();
        var sut = BuildSut(repository);

        repository.GetGlobalConversationAsync().Returns((ChatConversationDto?)null);
        repository.SaveAsync(Arg.Any<ChatConversation>()).Returns(createdGlobalConversation);

        // when
        var actual = await sut.GetOrCreateGlobalConversationAsync();

        // then
        actual.Should().Be(createdGlobalConversation);
        await repository.Received(1).SaveAsync(Arg.Is<ChatConversation>(c => c.IsGlobal));
    }

    // ── DeleteConversationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteConversationAsync_ShouldReturnTrue_WhenRequesterIsAdminAndConversationIsDeleted()
    {
        // given
        var conversationId = Guid.NewGuid();
        var requesterParticipantId = Guid.NewGuid();
        var repository = Substitute.For<IConversationRepository>();
        var participantService = Substitute.For<IConversationParticipantService>();
        var sut = BuildSut(repository, participantService);

        participantService.GetAllParticipantsByConversationIdAsync(conversationId).Returns([
            new ConversationParticipantDto(requesterParticipantId, "admin-user", ConversationParticipantRole.Admin, null)
        ]);
        repository.DeleteAsync(conversationId).Returns(true);

        // when
        var actual = await sut.DeleteConversationAsync(conversationId, requesterParticipantId);

        // then
        actual.Should().BeTrue();
        await repository.Received(1).DeleteAsync(conversationId);
    }

    [Fact]
    public async Task DeleteConversationAsync_ShouldReturnFalse_WhenRequesterIsNotAdmin()
    {
        // given
        var conversationId = Guid.NewGuid();
        var requesterParticipantId = Guid.NewGuid();
        var repository = Substitute.For<IConversationRepository>();
        var participantService = Substitute.For<IConversationParticipantService>();
        var sut = BuildSut(repository, participantService);

        participantService.GetAllParticipantsByConversationIdAsync(conversationId).Returns([
            new ConversationParticipantDto(requesterParticipantId, "regular-user", ConversationParticipantRole.Regular, null)
        ]);

        // when
        var actual = await sut.DeleteConversationAsync(conversationId, requesterParticipantId);

        // then
        actual.Should().BeFalse();
        await repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }
}
